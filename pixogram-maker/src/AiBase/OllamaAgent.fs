module AiBase.OllamaAgent

open System
open System.IO
open System.Net.Http
open System.Text
open System.Text.Json
open AiBase.Agent

type OllamaConfig =
    {
        BaseUrl: string
        Model: string
        ApiKey: string option
        // Enables native reasoning (qwen3.x/gemma4 thinking mode). Disable for
        // structured routing prompts where qwen3.x falls into self-reinforcement loops.
        Think: bool
        // Context window size (num_ctx). Must be sent explicitly so the server allocates
        // a stable KV-cache slot — without it, Ollama uses a tiny default (2–4k) that
        // truncates our ~15k prompts AND invalidates prefix-cache hits across calls.
        NumCtx: int
    }

let listModels (baseUrl: string) : Async<string list> =
    async {
        use client = new HttpClient()
        let! json = client.GetStringAsync($"{baseUrl}/api/tags") |> Async.AwaitTask
        use doc = JsonDocument.Parse(json)
        return
            doc.RootElement.GetProperty("models").EnumerateArray()
            |> Seq.map (fun m -> m.GetProperty("name").GetString())
            |> Seq.toList
    }

let listRunningModels (baseUrl: string) : Async<string list> =
    async {
        use client = new HttpClient()
        let! json = client.GetStringAsync($"{baseUrl}/api/ps") |> Async.AwaitTask
        use doc = JsonDocument.Parse(json)
        return
            doc.RootElement.GetProperty("models").EnumerateArray()
            |> Seq.map (fun m -> m.GetProperty("name").GetString())
            |> Seq.toList
    }

type OllamaAgent(config: OllamaConfig) =
    let client = new HttpClient(Timeout = TimeSpan.FromMinutes(30.0))
    let mutable disposed = false

    let buildRequestJson (messages: ChatMessage list) =
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream)
        writer.WriteStartObject()
        writer.WriteString("model", config.Model)
        writer.WritePropertyName("messages")
        writer.WriteStartArray()
        for msg in messages do
            writer.WriteStartObject()
            writer.WriteString("role", msg.Role)
            writer.WriteString("content", msg.Content)
            writer.WriteEndObject()
        writer.WriteEndArray()
        writer.WriteBoolean("stream", true)
        // Reasoning toggle: qwen3.x falls into self-reinforcement loops on structured
        // routing prompts (e.g. Triage). For generation-heavy roles (Implementor, Director),
        // thinking is essential for following multi-turn feedback. Caller decides per agent.
        writer.WriteBoolean("think", config.Think)
        writer.WritePropertyName("options")
        writer.WriteStartObject()
        writer.WriteNumber("num_ctx", config.NumCtx)
        writer.WriteEndObject()
        writer.WriteEndObject()
        writer.Flush()
        Encoding.UTF8.GetString(stream.ToArray())

    interface IAgent with
        member _.SendChat(messages, onEvent) =
            async {
                let json = buildRequestJson messages
                onEvent (RawRequest json)
                let content = new StringContent(json, Encoding.UTF8, "application/json")
                use request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/api/chat", Content = content)
                match config.ApiKey with
                | Some key when key <> "" ->
                    request.Headers.Authorization <- System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key)
                | _ -> ()

                let! response =
                    client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                    |> Async.AwaitTask

                let! responseStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                use reader = new StreamReader(responseStream)
                let fullResponse = StringBuilder()
                let mutable isDone = false

                while not isDone do
                    let! line = reader.ReadLineAsync() |> Async.AwaitTask
                    if isNull line then
                        isDone <- true
                    elif line <> "" then
                        onEvent (RawEvent line)
                        try
                            use doc = JsonDocument.Parse(line)
                            let root = doc.RootElement

                            let isFinished =
                                match root.TryGetProperty("done") with
                                | true, v -> v.GetBoolean()
                                | _ -> false

                            match root.TryGetProperty("message") with
                            | true, msg ->
                                // Gemma 4 thinking mode: stream thinking tokens separately
                                match msg.TryGetProperty("thinking") with
                                | true, t ->
                                    let token = t.GetString()
                                    if not (String.IsNullOrEmpty token) then
                                        onEvent (Thinking token)
                                | _ -> ()
                                match msg.TryGetProperty("content") with
                                | true, c ->
                                    let token = c.GetString()
                                    if not (String.IsNullOrEmpty token) then
                                        fullResponse.Append(token) |> ignore
                                        onEvent (Text token)
                                | _ -> ()
                            | _ -> ()

                            if isFinished then
                                // Extract performance metrics from final chunk
                                let getInt64 (name: string) =
                                    match root.TryGetProperty(name) with
                                    | true, v -> v.GetInt64() | _ -> 0L
                                let promptEvalCount = getInt64 "prompt_eval_count"
                                let evalCount = getInt64 "eval_count"
                                let promptEvalDur = getInt64 "prompt_eval_duration"
                                let evalDur = getInt64 "eval_duration"
                                let totalDur = getInt64 "total_duration"
                                // prompt_eval_count is the TOTAL prompt size, not tokens actually recomputed —
                                // on a cache hit, the cached prefix is skipped but still counted. So tok/s is
                                // meaningless as a cache-hit indicator. Log prompt_eval_duration directly: that
                                // shrinks toward zero on a warm cache and grows with new-token work.
                                let promptDurSec = float promptEvalDur / 1e9
                                let evalTokPerSec = if evalDur > 0L then float evalCount / (float evalDur / 1e9) else 0.0
                                eprintfn $"    [ollama] prompt_eval: {promptEvalCount} tok in {promptDurSec:F2}s, gen: {evalCount} tok @ {evalTokPerSec:F1} tok/s, total: {float totalDur / 1e9:F1}s"
                                onEvent (Metrics {
                                    PromptEvalCount = promptEvalCount
                                    EvalCount = evalCount
                                    PromptEvalDurNs = promptEvalDur
                                    EvalDurNs = evalDur
                                    TotalDurNs = totalDur
                                })
                                isDone <- true
                        with _ -> ()

                let result = fullResponse.ToString()
                onEvent (Result result)
                return result
            }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                client.Dispose()
