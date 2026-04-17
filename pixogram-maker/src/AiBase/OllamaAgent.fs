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
        // Disable reasoning: qwen3.x falls into self-reinforcement loops ("IMPLEMENTOR. Wait! ...")
        // that never terminate on longer prompts. Direct answers are both faster and more reliable.
        writer.WriteBoolean("think", false)
        writer.WriteEndObject()
        writer.Flush()
        Encoding.UTF8.GetString(stream.ToArray())

    interface IAgent with
        member _.SendChat(messages, onEvent) =
            async {
                let json = buildRequestJson messages
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
                                let promptTokPerSec = if promptEvalDur > 0L then float promptEvalCount / (float promptEvalDur / 1e9) else 0.0
                                let evalTokPerSec = if evalDur > 0L then float evalCount / (float evalDur / 1e9) else 0.0
                                eprintfn $"    [ollama] prompt_eval: {promptEvalCount} tokens ({promptTokPerSec:F1} tok/s), gen: {evalCount} tokens ({evalTokPerSec:F1} tok/s), total: {float totalDur / 1e9:F1}s"
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
