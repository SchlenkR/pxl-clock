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
        SystemPrompt: string option
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
    let client = new HttpClient(Timeout = TimeSpan.FromMinutes(10.0))
    let history = ResizeArray<string * string>()
    let mutable disposed = false

    do
        match config.SystemPrompt with
        | Some sp -> history.Add("system", sp)
        | None -> ()

    let buildRequestJson () =
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream)
        writer.WriteStartObject()
        writer.WriteString("model", config.Model)
        writer.WritePropertyName("messages")
        writer.WriteStartArray()
        for role, content in history do
            writer.WriteStartObject()
            writer.WriteString("role", role)
            writer.WriteString("content", content)
            writer.WriteEndObject()
        writer.WriteEndArray()
        writer.WriteBoolean("stream", true)
        writer.WriteEndObject()
        writer.Flush()
        Encoding.UTF8.GetString(stream.ToArray())

    interface IAgent with
        member _.Send(prompt, onEvent) =
            async {
                history.Add("user", prompt)
                let json = buildRequestJson()
                let content = new StringContent(json, Encoding.UTF8, "application/json")
                use request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/api/chat", Content = content)

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
                                match msg.TryGetProperty("content") with
                                | true, c ->
                                    let token = c.GetString()
                                    if not (String.IsNullOrEmpty token) then
                                        fullResponse.Append(token) |> ignore
                                        onEvent (Text token)
                                | _ -> ()
                            | _ -> ()

                            if isFinished then isDone <- true
                        with _ -> ()

                let result = fullResponse.ToString()
                history.Add("assistant", result)
                onEvent (Result result)
                return result
            }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                client.Dispose()
