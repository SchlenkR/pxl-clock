module AiBase.AnthropicAgent

open System
open System.IO
open System.Net.Http
open System.Text
open System.Text.Json
open AiBase.Agent

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

type AnthropicConfig =
    {
        ApiKey: string
        Model: string
        MaxTokens: int
        SystemPrompt: string option
    }

let defaultAnthropicConfig =
    {
        ApiKey = ""
        Model = "claude-sonnet-4-5-20241022"
        MaxTokens = 8192
        SystemPrompt = None
    }

// ---------------------------------------------------------------------------
// AnthropicAgent — direct HTTP streaming to Anthropic Messages API
// ---------------------------------------------------------------------------

type AnthropicAgent(config: AnthropicConfig) =
    let client = new HttpClient(Timeout = TimeSpan.FromMinutes(5.0))
    let history = ResizeArray<string * string>()
    let mutable disposed = false

    let log msg = eprintfn $"    [anthropic] {msg}"

    do
        if String.IsNullOrWhiteSpace config.ApiKey then
            failwith "AnthropicAgent: CLAUDE_API_KEY is not set"
        log $"Agent created (model: {config.Model}, maxTokens: {config.MaxTokens})"

    let buildRequestJson () =
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream)
        writer.WriteStartObject()
        writer.WriteString("model", config.Model)
        writer.WriteNumber("max_tokens", config.MaxTokens)
        writer.WriteBoolean("stream", true)

        match config.SystemPrompt with
        | Some sp when sp <> "" ->
            writer.WritePropertyName("system")
            writer.WriteStartArray()
            writer.WriteStartObject()
            writer.WriteString("type", "text")
            writer.WriteString("text", sp)
            writer.WriteEndObject()
            writer.WriteEndArray()
        | _ -> ()

        writer.WritePropertyName("messages")
        writer.WriteStartArray()
        for role, content in history do
            writer.WriteStartObject()
            writer.WriteString("role", role)
            writer.WriteString("content", content)
            writer.WriteEndObject()
        writer.WriteEndArray()

        writer.WriteEndObject()
        writer.Flush()
        Encoding.UTF8.GetString(stream.ToArray())

    interface IAgent with
        member _.Send(prompt, onEvent) =
            async {
                history.Add("user", prompt)
                log $"Sending prompt ({prompt.Length} chars)..."

                let json = buildRequestJson()
                let content = new StringContent(json, Encoding.UTF8, "application/json")
                use request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
                request.Content <- content
                request.Headers.Add("x-api-key", config.ApiKey)
                request.Headers.Add("anthropic-version", "2023-06-01")

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
                let mutable currentBlockType = ""
                let mutable currentToolName = ""
                let toolInput = StringBuilder()

                while not isDone do
                    let! line = reader.ReadLineAsync() |> Async.AwaitTask
                    if isNull line then
                        isDone <- true
                    elif line.StartsWith("event: ") then
                        let eventType = line.Substring(7).Trim()
                        if eventType = "message_stop" then
                            isDone <- true
                    elif line.StartsWith("data: ") then
                        let data = line.Substring(6)
                        try
                            use doc = JsonDocument.Parse(data)
                            let root = doc.RootElement

                            match root.TryGetProperty("type") with
                            | true, t ->
                                match t.GetString() with
                                | "content_block_start" ->
                                    match root.TryGetProperty("content_block") with
                                    | true, block ->
                                        match block.TryGetProperty("type") with
                                        | true, bt ->
                                            currentBlockType <- bt.GetString()
                                            if currentBlockType = "tool_use" then
                                                currentToolName <-
                                                    match block.TryGetProperty("name") with
                                                    | true, n -> n.GetString()
                                                    | _ -> "?"
                                                toolInput.Clear() |> ignore
                                        | _ -> ()
                                    | _ -> ()

                                | "content_block_delta" ->
                                    match root.TryGetProperty("delta") with
                                    | true, delta ->
                                        match delta.TryGetProperty("type") with
                                        | true, dt ->
                                            match dt.GetString() with
                                            | "text_delta" ->
                                                match delta.TryGetProperty("text") with
                                                | true, text ->
                                                    let token = text.GetString()
                                                    if not (String.IsNullOrEmpty token) then
                                                        fullResponse.Append(token) |> ignore
                                                        onEvent (Text token)
                                                | _ -> ()
                                            | "thinking_delta" ->
                                                match delta.TryGetProperty("thinking") with
                                                | true, text ->
                                                    let token = text.GetString()
                                                    if not (String.IsNullOrEmpty token) then
                                                        onEvent (Thinking token)
                                                | _ -> ()
                                            | "input_json_delta" ->
                                                match delta.TryGetProperty("partial_json") with
                                                | true, json ->
                                                    let chunk = json.GetString()
                                                    if not (String.IsNullOrEmpty chunk) then
                                                        toolInput.Append(chunk) |> ignore
                                                | _ -> ()
                                            | other ->
                                                log $"Unknown delta type: {other}"
                                        | _ -> ()
                                    | _ -> ()

                                | "content_block_stop" ->
                                    if currentBlockType = "tool_use" then
                                        let input = toolInput.ToString()
                                        onEvent (ToolUse(currentToolName, input))
                                        toolInput.Clear() |> ignore
                                    currentBlockType <- ""

                                | "error" ->
                                    match root.TryGetProperty("error") with
                                    | true, err ->
                                        let msg =
                                            match err.TryGetProperty("message") with
                                            | true, m -> m.GetString()
                                            | _ -> err.GetRawText()
                                        log $"ERROR: {msg}"
                                        onEvent (Error msg)
                                    | _ -> ()

                                | other ->
                                    log $"Unknown event: {other}"
                            | _ -> ()
                        with _ -> ()

                let result = fullResponse.ToString()
                log $"Response received ({result.Length} chars)"
                history.Add("assistant", result)
                onEvent (Result result)
                return result
            }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                log "Disposing"
                client.Dispose()
