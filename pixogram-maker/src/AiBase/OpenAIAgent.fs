module AiBase.OpenAIAgent

open System
open System.IO
open System.Net.Http
open System.Text
open System.Text.Json
open AiBase.Agent

// ---------------------------------------------------------------------------
// Generic OpenAI-compatible Chat Completions agent.
//
// Works with: OpenRouter, OpenAI direct, Together, Groq, DeepSeek, vLLM,
// Ollama's `/v1/chat/completions` endpoint — anything speaking the OpenAI dialect.
//
// OpenRouter-specific knobs (HTTP-Referer, X-Title, reasoning.effort) are exposed
// as optional config — harmless on other providers (they either ignore unknown
// fields or accept the headers).
// ---------------------------------------------------------------------------

type OpenAIConfig =
    {
        ApiKey: string
        BaseUrl: string                 // e.g. "https://openrouter.ai/api/v1"
        Model: string                   // e.g. "minimax/minimax-m2.5"
        Effort: Effort
        MaxTokens: int option           // None → let the provider decide
        // Controls whether `reasoning: { effort }` is sent. Default true.
        // Turn off for backends that reject unknown fields (rare).
        IncludeReasoning: bool
        // OpenRouter rankings — optional. Values land in HTTP-Referer / X-Title.
        Referer: string option
        Title: string option
    }

let defaultOpenAIConfig =
    {
        ApiKey = ""
        BaseUrl = "https://openrouter.ai/api/v1"
        Model = "openrouter/auto"
        Effort = Medium
        MaxTokens = None
        IncludeReasoning = true
        Referer = Some "https://github.com/SchlenkR/pxl-clock"
        Title = Some "PXL Clock Pixogram Maker"
    }

let private effortString =
    function
    | Low -> "low"
    | Medium -> "medium"
    | High -> "high"
    | Max -> "high"     // OpenRouter only supports low/medium/high — map Max→high

type OpenAIAgent(config: OpenAIConfig) =
    let client = new HttpClient(Timeout = TimeSpan.FromMinutes(10.0))
    let mutable disposed = false

    let log msg = eprintfn $"    [openai] {msg}"

    do
        if String.IsNullOrWhiteSpace config.ApiKey then
            failwith "OpenAIAgent: ApiKey is empty"
        log $"Agent created (base: {config.BaseUrl}, model: {config.Model})"

    let buildRequestJson (messages: ChatMessage list) =
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream)
        writer.WriteStartObject()
        writer.WriteString("model", config.Model)
        writer.WriteBoolean("stream", true)

        // stream_options.include_usage: OpenAI/OpenRouter convention to emit
        // a final chunk with usage stats. Without it we get no token counts.
        writer.WritePropertyName("stream_options")
        writer.WriteStartObject()
        writer.WriteBoolean("include_usage", true)
        writer.WriteEndObject()

        match config.MaxTokens with
        | Some n ->
            writer.WriteNumber("max_tokens", n)
        | None -> ()

        if config.IncludeReasoning then
            // OpenRouter's unified reasoning control. Ignored by providers that
            // don't support reasoning; applied transparently for those that do
            // (o-series, Claude thinking, Gemini thinking, DeepSeek-R1 style, …).
            writer.WritePropertyName("reasoning")
            writer.WriteStartObject()
            writer.WriteString("effort", effortString config.Effort)
            writer.WriteEndObject()

        writer.WritePropertyName("messages")
        writer.WriteStartArray()
        for msg in messages do
            writer.WriteStartObject()
            writer.WriteString("role", msg.Role)
            writer.WriteString("content", msg.Content)
            writer.WriteEndObject()
        writer.WriteEndArray()

        writer.WriteEndObject()
        writer.Flush()
        Encoding.UTF8.GetString(stream.ToArray())

    let parseUsage (root: JsonElement) : struct (int64 * int64) =
        match root.TryGetProperty("usage") with
        | true, u ->
            let promptTok =
                match u.TryGetProperty("prompt_tokens") with
                | true, v when v.ValueKind = JsonValueKind.Number -> v.GetInt64()
                | _ -> 0L
            let completionTok =
                match u.TryGetProperty("completion_tokens") with
                | true, v when v.ValueKind = JsonValueKind.Number -> v.GetInt64()
                | _ -> 0L
            struct (promptTok, completionTok)
        | _ -> struct (0L, 0L)

    interface IAgent with
        member _.SendChat(messages, onEvent) =
            async {
                log $"Sending {messages.Length} messages..."

                let json = buildRequestJson messages
                onEvent (RawRequest json)
                let content = new StringContent(json, Encoding.UTF8, "application/json")
                let endpoint = sprintf "%s/chat/completions" (config.BaseUrl.TrimEnd('/'))
                use request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                request.Content <- content
                request.Headers.Authorization <-
                    System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey)
                match config.Referer with
                | Some r -> request.Headers.Add("HTTP-Referer", r)
                | None -> ()
                match config.Title with
                | Some t -> request.Headers.Add("X-Title", t)
                | None -> ()

                let! response =
                    client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                    |> Async.AwaitTask

                if not response.IsSuccessStatusCode then
                    let! errorBody = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                    let msg = $"HTTP {int response.StatusCode}: {errorBody}"
                    log $"ERROR: {msg}"
                    onEvent (Error msg)
                    return ""
                else

                let! responseStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                use reader = new StreamReader(responseStream)
                let fullResponse = StringBuilder()
                let mutable isDone = false
                let mutable inputTokens = 0L
                let mutable outputTokens = 0L
                let stopwatch = System.Diagnostics.Stopwatch.StartNew()
                let mutable firstTokenAtMs = 0L

                while not isDone do
                    let! line = reader.ReadLineAsync() |> Async.AwaitTask
                    if isNull line then
                        isDone <- true
                    else
                        onEvent (RawEvent line)
                        // SSE: blank lines separate events, comments start with ':'.
                        if line = "" || line.StartsWith(":") then
                            ()
                        elif line.StartsWith("data: ") then
                            let data = line.Substring(6)
                            if data = "[DONE]" then
                                isDone <- true
                            else
                                try
                                    use doc = JsonDocument.Parse(data)
                                    let root = doc.RootElement

                                    // Usage (only on final chunk when stream_options.include_usage=true)
                                    let struct (pIn, pOut) = parseUsage root
                                    if pIn > 0L then inputTokens <- pIn
                                    if pOut > 0L then outputTokens <- pOut

                                    // Choices.delta
                                    match root.TryGetProperty("choices") with
                                    | true, choices when choices.ValueKind = JsonValueKind.Array ->
                                        for choice in choices.EnumerateArray() do
                                            match choice.TryGetProperty("delta") with
                                            | true, delta ->
                                                // Reasoning (OpenRouter normalized field).
                                                // Some providers stream reasoning_content (DeepSeek),
                                                // OpenRouter rewrites to `reasoning`. Handle both.
                                                let reasoningText =
                                                    match delta.TryGetProperty("reasoning") with
                                                    | true, r when r.ValueKind = JsonValueKind.String ->
                                                        r.GetString()
                                                    | _ ->
                                                        match delta.TryGetProperty("reasoning_content") with
                                                        | true, r when r.ValueKind = JsonValueKind.String ->
                                                            r.GetString()
                                                        | _ -> null
                                                if not (String.IsNullOrEmpty reasoningText) then
                                                    if firstTokenAtMs = 0L then
                                                        firstTokenAtMs <- stopwatch.ElapsedMilliseconds
                                                    onEvent (Thinking reasoningText)

                                                // Regular content
                                                match delta.TryGetProperty("content") with
                                                | true, c when c.ValueKind = JsonValueKind.String ->
                                                    let token = c.GetString()
                                                    if not (String.IsNullOrEmpty token) then
                                                        if firstTokenAtMs = 0L then
                                                            firstTokenAtMs <- stopwatch.ElapsedMilliseconds
                                                        fullResponse.Append(token) |> ignore
                                                        onEvent (Text token)
                                                | _ -> ()
                                            | _ -> ()

                                            // finish_reason — informational; we rely on [DONE] / end-of-stream
                                            ()
                                    | _ -> ()

                                    // Top-level error envelope (OpenRouter returns 200 + {error:{message}} in-stream sometimes)
                                    match root.TryGetProperty("error") with
                                    | true, err ->
                                        let msg =
                                            match err.TryGetProperty("message") with
                                            | true, m when m.ValueKind = JsonValueKind.String -> m.GetString()
                                            | _ -> err.GetRawText()
                                        log $"ERROR: {msg}"
                                        onEvent (Error msg)
                                    | _ -> ()
                                with ex ->
                                    log $"Failed to parse chunk: {ex.Message}"
                        else
                            // Unknown SSE line (field: value) — already captured via RawEvent above.
                            ()

                stopwatch.Stop()
                let totalNs = stopwatch.ElapsedMilliseconds * 1_000_000L
                let prefillNs =
                    (if firstTokenAtMs > 0L then firstTokenAtMs
                     else stopwatch.ElapsedMilliseconds) * 1_000_000L
                let evalNs = max 0L (totalNs - prefillNs)
                onEvent (Metrics {
                    PromptEvalCount = inputTokens
                    EvalCount = outputTokens
                    PromptEvalDurNs = prefillNs
                    EvalDurNs = evalNs
                    TotalDurNs = totalNs
                })
                let result = fullResponse.ToString()
                log $"Response received ({result.Length} chars)"
                onEvent (Result result)
                return result
            }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                log "Disposing"
                client.Dispose()
