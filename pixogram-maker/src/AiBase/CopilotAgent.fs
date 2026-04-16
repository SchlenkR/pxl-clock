module AiBase.CopilotAgent

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open AiBase.Agent

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

type CopilotConfig =
    {
        Model: string
        Effort: Effort
        WorkingDirectory: string
        AvailableTools: string list
    }

let defaultCopilotConfig =
    {
        Model = "claude-sonnet-4.5"
        Effort = Medium
        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        AvailableTools = []
    }

// ---------------------------------------------------------------------------
// Model discovery
// ---------------------------------------------------------------------------

let private findCopilotSdkPath () =
    let tryDir dir =
        let sdk = Path.Combine(dir, "copilot-sdk")
        if Directory.Exists(sdk) then Some sdk else None

    // 1. Try npm global root
    let fromNpm () =
        try
            let psi = ProcessStartInfo("/bin/sh")
            psi.ArgumentList.Add("-c")
            psi.ArgumentList.Add("npm root -g")
            psi.RedirectStandardOutput <- true
            psi.UseShellExecute <- false
            psi.CreateNoWindow <- true
            use p = Process.Start(psi)
            let root = p.StandardOutput.ReadToEnd().Trim()
            p.WaitForExit(5000) |> ignore
            if p.ExitCode = 0 && root <> "" then
                tryDir (Path.Combine(root, "@github", "copilot"))
            else None
        with _ -> None

    // 2. Try well-known homebrew paths
    let fromHomebrew () =
        [ "/opt/homebrew/lib/node_modules/@github/copilot"
          "/usr/local/lib/node_modules/@github/copilot" ]
        |> List.tryPick tryDir

    fromNpm() |> Option.orElseWith fromHomebrew

let listModels () : string list =
    match findCopilotSdkPath() with
    | None -> []
    | Some sdkPath ->
    try
        let script =
            "const {CopilotClient} = require('" + sdkPath.Replace("'", "\\'") + "');"
            + "const c = new CopilotClient();"
            + "c.start().then(async () => { (await c.listModels()).forEach(m => console.log(m.id)); process.exit(0); }).catch(() => process.exit(1));"
        let psi = ProcessStartInfo("node")
        psi.ArgumentList.Add("-e")
        psi.ArgumentList.Add(script)
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        use p = Process.Start(psi)
        let output = p.StandardOutput.ReadToEnd()
        p.WaitForExit(15000) |> ignore
        if p.ExitCode = 0 then
            output.Trim().Split('\n')
            |> Array.map (fun s -> s.Trim())
            |> Array.filter (fun s -> s <> "")
            |> Array.toList
        else []
    with _ -> []

// ---------------------------------------------------------------------------
// CopilotAgent — communicates via ACP (JSON-RPC over NDJSON stdio)
// ---------------------------------------------------------------------------

type CopilotAgent(config: CopilotConfig) =
    let mutable requestId = 0
    let mutable sessionId = ""

    let mutable disposed = false

    let nextId () =
        let id = requestId
        requestId <- requestId + 1
        id

    let copilotPath =
        Environment.GetEnvironmentVariable("COPILOT_PATH")
        |> Option.ofObj
        |> Option.defaultValue "copilot"

    let effortStr =
        match config.Effort with
        | Low -> "low" | Medium -> "medium" | High -> "high" | Max -> "high"

    let log msg = eprintfn $"    [copilot] {msg}"

    let proc =
        let psi = ProcessStartInfo(copilotPath)
        psi.ArgumentList.Add("--acp")
        psi.ArgumentList.Add("--stdio")
        if config.Model <> "" then
            psi.ArgumentList.Add("--model")
            psi.ArgumentList.Add(config.Model)
        psi.ArgumentList.Add("--effort")
        psi.ArgumentList.Add(effortStr)
        psi.ArgumentList.Add("--available-tools")
        if config.AvailableTools <> [] then
            psi.ArgumentList.Add(String.Join(",", config.AvailableTools))
        else
            psi.ArgumentList.Add("")
        psi.RedirectStandardInput <- true
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        let args = String.Join(" ", psi.ArgumentList)
        log $"Starting: {copilotPath} {args}"
        let p = Process.Start(psi)
        log $"Process started (PID {p.Id})"
        p

    // -- JSON-RPC helpers --

    let sendRpc (method: string) (paramsObj: JsonElement) =
        let id = nextId()
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream)
        writer.WriteStartObject()
        writer.WriteString("jsonrpc", "2.0")
        writer.WriteNumber("id", id)
        writer.WriteString("method", method)
        writer.WritePropertyName("params")
        paramsObj.WriteTo(writer)
        writer.WriteEndObject()
        writer.Flush()
        let msg = Encoding.UTF8.GetString(stream.ToArray())
        proc.StandardInput.WriteLine(msg)
        proc.StandardInput.Flush()
        id

    let buildJson (build: Utf8JsonWriter -> unit) =
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream)
        build writer
        writer.Flush()
        JsonDocument.Parse(stream.ToArray()).RootElement.Clone()

    let readResponseFor (expectedId: int) : Async<Result<JsonElement, string>> =
        async {
            let mutable found = false
            let mutable response: Result<JsonElement, string> = Result.Error "No response"
            while not found do
                let! line =
                    async {
                        try
                            let! l = proc.StandardOutput.ReadLineAsync() |> Async.AwaitTask
                            return l
                        with _ -> return null
                    }
                if isNull line then
                    found <- true
                else
                    try
                        use doc = JsonDocument.Parse(line)
                        let root = doc.RootElement
                        match Json.tryProp "id" root with
                        | Some idEl when idEl.GetInt32() = expectedId ->
                            match Json.tryProp "error" root with
                            | Some err ->
                                let msg =
                                    Json.tryProp "message" err
                                    |> Option.bind Json.str
                                    |> Option.defaultValue (err.GetRawText())
                                response <- Result.Error msg
                            | None ->
                                match Json.tryProp "result" root with
                                | Some r -> response <- Ok (r.Clone())
                                | None -> response <- Result.Error "Empty result"
                            found <- true
                        | _ -> ()
                    with _ -> ()
            return response
        }

    // -- Initialize + create session --

    let initialize () =
        async {
            log "Sending initialize..."
            let initParams = buildJson (fun w ->
                w.WriteStartObject()
                w.WriteNumber("protocolVersion", 1)
                w.WriteStartObject("clientCapabilities")
                w.WriteEndObject()
                w.WriteStartObject("clientInfo")
                w.WriteString("name", "AiBase")
                w.WriteString("version", "1.0.0")
                w.WriteEndObject()
                w.WriteEndObject())
            let initId = sendRpc "initialize" initParams
            match! readResponseFor initId with
            | Result.Error e -> failwith $"Copilot initialize failed: {e}"
            | Ok _ -> log "Initialize OK"

            log "Creating session..."
            let sessionParams = buildJson (fun w ->
                w.WriteStartObject()
                w.WriteString("cwd", config.WorkingDirectory)
                w.WritePropertyName("mcpServers")
                w.WriteStartArray()
                w.WriteEndArray()
                w.WriteEndObject())
            let sessionReqId = sendRpc "session/new" sessionParams
            match! readResponseFor sessionReqId with
            | Result.Error e -> failwith $"Copilot session/new failed: {e}"
            | Ok result ->
                sessionId <-
                    Json.tryProp "sessionId" result
                    |> Option.bind Json.str
                    |> Option.defaultValue ""
                log $"Session created: {sessionId}"
        }

    do initialize () |> Async.RunSynchronously

    let cleanup () =
        if not disposed then
            disposed <- true
            log $"Disposing (PID {proc.Id})..."
            try proc.StandardInput.Close() with _ -> ()
            if not proc.HasExited then
                try proc.Kill() with _ -> ()
            proc.Dispose()

    do
        AppDomain.CurrentDomain.ProcessExit.Add(fun _ -> cleanup())
        Console.CancelKeyPress.Add(fun e ->
            e.Cancel <- true
            cleanup())

    // -- Parse ACP session/update events --

    /// Returns true if a fatal error occurred and the loop should stop.
    let processUpdate (update: JsonElement) (fullText: StringBuilder) (onEvent: AgentEvent -> unit) : bool =
        let updateType = Json.tryProp "sessionUpdate" update |> Option.bind Json.str
        match updateType with
        | Some "agent_message_chunk" ->
            let content = Json.tryProp "content" update
            match content with
            | Some c ->
                let cType = Json.tryProp "type" c |> Option.bind Json.str
                match cType with
                | Some "text" ->
                    let text = Json.tryProp "text" c |> Option.bind Json.str |> Option.defaultValue ""
                    if text <> "" then
                        fullText.Append(text) |> ignore
                        onEvent (Text text)
                | Some "thinking" ->
                    let text = Json.tryProp "text" c |> Option.bind Json.str |> Option.defaultValue ""
                    if text <> "" then onEvent (Thinking text)
                | _ -> ()
            | None -> ()
            false
        | Some "tool_call" ->
            let title = Json.tryProp "title" update |> Option.bind Json.str |> Option.defaultValue "?"
            let toolId = Json.tryProp "toolCallId" update |> Option.bind Json.str |> Option.defaultValue ""
            onEvent (ToolUse(title, toolId))
            false
        | Some "tool_call_update" ->
            let status = Json.tryProp "status" update |> Option.bind Json.str
            if status = Some "completed" then
                let contentArr = Json.tryProp "content" update
                let resultText =
                    match contentArr with
                    | Some c when c.ValueKind = JsonValueKind.Array ->
                        c.EnumerateArray()
                        |> Seq.tryHead
                        |> Option.bind (Json.tryProp "content")
                        |> Option.bind (fun inner ->
                            Json.tryProp "text" inner |> Option.bind Json.str)
                        |> Option.defaultValue ""
                    | _ -> ""
                if resultText <> "" then onEvent (ToolResult resultText)
            false
        | Some unknown ->
            log $"Fatal session update: {unknown}"
            onEvent (Error $"Copilot session error: {unknown}")
            true
        | None ->
            let raw = update.GetRawText()
            log $"Session update without type: {raw}"
            false

    interface IAgent with
        member _.SendChat(messages, onEvent) =
            async {
                let prompt = ChatMessage.formatAsText messages
                let promptLen = prompt.Length
                log $"Sending prompt ({promptLen} chars)..."

                let actualPrompt = prompt

                let promptParams = buildJson (fun w ->
                    w.WriteStartObject()
                    w.WriteString("sessionId", sessionId)
                    w.WritePropertyName("prompt")
                    w.WriteStartArray()
                    w.WriteStartObject()
                    w.WriteString("type", "text")
                    w.WriteString("text", actualPrompt)
                    w.WriteEndObject()
                    w.WriteEndArray()
                    w.WriteEndObject())

                let! sendOk =
                    async {
                        try
                            let id = sendRpc "session/prompt" promptParams
                            return Some id
                        with ex ->
                            onEvent (Error $"Copilot process died: {ex.Message}")
                            return None
                    }

                match sendOk with
                | None -> return ""
                | Some promptId ->

                let fullText = StringBuilder()
                let mutable isDone = false

                while not isDone do
                    let! line =
                        async {
                            try
                                let! l = proc.StandardOutput.ReadLineAsync() |> Async.AwaitTask
                                return l
                            with ex ->
                                onEvent (Error $"Copilot process died: {ex.Message}")
                                return null
                        }

                    if isNull line then
                        isDone <- true
                    else
                        try
                            use doc = JsonDocument.Parse(line)
                            let root = doc.RootElement

                            match Json.tryProp "id" root with
                            | Some idEl when idEl.GetInt32() = promptId ->
                                // Final response — check for error
                                match Json.tryProp "error" root with
                                | Some err ->
                                    let msg =
                                        Json.tryProp "message" err
                                        |> Option.bind Json.str
                                        |> Option.defaultValue (err.GetRawText())
                                    onEvent (Error msg)
                                | None -> ()
                                isDone <- true
                            | Some reqId ->
                                // Server request — check for permission
                                let method = Json.tryProp "method" root |> Option.bind Json.str
                                match method with
                                | Some "session/request_permission" ->
                                    // Auto-approve tool permissions
                                    let respId = reqId.GetInt32()
                                    let approveMsg = $"""{{ "jsonrpc":"2.0","id":{respId},"result":{{ "permission":"allow" }} }}"""
                                    try
                                        proc.StandardInput.WriteLine(approveMsg)
                                        proc.StandardInput.Flush()
                                    with _ -> ()
                                | _ -> ()
                            | None ->
                                // Notification (no id) — check for session/update
                                let method = Json.tryProp "method" root |> Option.bind Json.str
                                match method with
                                | Some "session/update" ->
                                    let fatal =
                                        Json.tryProp "params" root
                                        |> Option.bind (Json.tryProp "update")
                                        |> Option.map (fun u -> processUpdate u fullText onEvent)
                                        |> Option.defaultValue false
                                    if fatal then isDone <- true
                                | _ -> ()
                        with _ -> ()

                let result = fullText.ToString()
                log $"Response received ({result.Length} chars)"
                onEvent (Result result)
                return result
            }

    interface IDisposable with
        member _.Dispose() = cleanup()
