module AiBase.Agent

open System
open System.Diagnostics
open System.IO
open System.Text.Json

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type Model =
    | Opus
    | Sonnet
    | Haiku
    | Custom of string

type Effort =
    | Low
    | Medium
    | High
    | Max

type AgentConfig =
    {
        Model: Model
        Effort: Effort
        WorkingDirectory: string
        AllowedTools: string list
        DockerImage: string option
        SharedFolder: string option
        MaxTurns: int option
        SystemPrompt: string option
    }

type AgentEvent =
    | Thinking of string
    | Text of string
    | ToolUse of string * string
    | ToolResult of string
    | Result of string
    | Error of string

type IAgent =
    inherit IDisposable
    abstract Send: prompt: string * onEvent: (AgentEvent -> unit) -> Async<string>

// ---------------------------------------------------------------------------
// Defaults
// ---------------------------------------------------------------------------

let defaultConfig =
    {
        Model = Sonnet
        Effort = Medium
        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        AllowedTools = [ "Read"; "Bash"; "Edit"; "Write"; "Glob"; "Grep" ]
        DockerImage = None
        SharedFolder = None
        MaxTurns = None
        SystemPrompt = None
    }

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private modelToString =
    function
    | Opus -> "opus"
    | Sonnet -> "sonnet"
    | Haiku -> "haiku"
    | Custom s -> s

let private effortToString =
    function
    | Low -> "low"
    | Medium -> "medium"
    | High -> "high"
    | Max -> "max"

module internal Json =
    let tryProp (name: string) (el: JsonElement) =
        match el.TryGetProperty(name) with
        | true, v -> Some v
        | _ -> None

    let str (el: JsonElement) =
        if el.ValueKind = JsonValueKind.String then Some(el.GetString()) else None

let private buildClaudeArgs (config: AgentConfig) =
    [
        "--input-format"; "stream-json"
        "--output-format"; "stream-json"
        "--verbose"
        "--model"; modelToString config.Model
        "--effort"; effortToString config.Effort

        for tool in config.AllowedTools do
            "--allowedTools"; tool

        match config.MaxTurns with
        | Some n -> "--max-turns"; string n
        | None -> ()

        match config.SystemPrompt with
        | Some sp -> "--append-system-prompt"; sp
        | None -> ()
    ]

let private createUserMessage (prompt: string) =
    let content = JsonSerializer.Serialize(prompt)
    $"""{{"type":"user","message":{{"role":"user","content":{content}}}}}"""

let private sendToProcess (stdin: StreamWriter) (stdout: StreamReader) (prompt: string) (onEvent: AgentEvent -> unit) : Async<string> =
    async {
        let msg = createUserMessage prompt

        let! sendOk =
            async {
                try
                    do! stdin.WriteLineAsync(msg) |> Async.AwaitTask
                    do! stdin.FlushAsync() |> Async.AwaitTask
                    return true
                with ex ->
                    onEvent (Error $"Agent process died: {ex.Message}")
                    return false
            }

        if not sendOk then return ""
        else

        let mutable resultText = ""
        let mutable isDone = false

        while not isDone do
            let! line = stdout.ReadLineAsync() |> Async.AwaitTask

            if isNull line then
                isDone <- true
            else
                try
                    use doc = JsonDocument.Parse(line)
                    let root = doc.RootElement

                    let msgType = Json.tryProp "type" root |> Option.bind Json.str

                    match msgType with
                    | Some "result" ->
                        let text =
                            Json.tryProp "result" root
                            |> Option.bind Json.str
                            |> Option.defaultValue ""
                        resultText <- text
                        onEvent (Result text)
                        isDone <- true
                    | Some "assistant" ->
                        let content =
                            Json.tryProp "message" root
                            |> Option.bind (Json.tryProp "content")

                        match content with
                        | Some c when c.ValueKind = JsonValueKind.Array ->
                            for item in c.EnumerateArray() do
                                let blockType = Json.tryProp "type" item |> Option.bind Json.str

                                match blockType with
                                | Some "thinking" ->
                                    let t =
                                        Json.tryProp "thinking" item
                                        |> Option.bind Json.str
                                        |> Option.defaultValue ""
                                    if t <> "" then onEvent (Thinking t)
                                | Some "text" ->
                                    let t =
                                        Json.tryProp "text" item
                                        |> Option.bind Json.str
                                        |> Option.defaultValue ""
                                    if t <> "" then onEvent (Text t)
                                | Some "tool_use" ->
                                    let name =
                                        Json.tryProp "name" item
                                        |> Option.bind Json.str
                                        |> Option.defaultValue "?"
                                    let input =
                                        Json.tryProp "input" item
                                        |> Option.map (fun e -> e.GetRawText())
                                        |> Option.defaultValue "{}"
                                    onEvent (ToolUse(name, input))
                                | Some "tool_result" ->
                                    let output =
                                        Json.tryProp "content" item
                                        |> Option.bind Json.str
                                        |> Option.defaultValue (
                                            Json.tryProp "content" item
                                            |> Option.map (fun e -> e.GetRawText())
                                            |> Option.defaultValue ""
                                        )
                                    if output <> "" then onEvent (ToolResult output)
                                | _ -> ()
                        | Some c when c.ValueKind = JsonValueKind.String ->
                            onEvent (Text(c.GetString()))
                        | _ -> ()
                    | _ -> ()
                with _ -> ()

        return resultText
    }

// ---------------------------------------------------------------------------
// ProcessAgent — runs claude CLI locally
// ---------------------------------------------------------------------------

type ProcessAgent(config: AgentConfig) =
    static let claudePath =
        Environment.GetEnvironmentVariable("CLAUDE_PATH")
        |> Option.ofObj
        |> Option.defaultValue "claude"

    let proc =
        let psi = ProcessStartInfo(claudePath)
        for arg in buildClaudeArgs config do
            psi.ArgumentList.Add(arg)
        psi.WorkingDirectory <- config.WorkingDirectory
        psi.RedirectStandardInput <- true
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        Process.Start(psi)

    let mutable disposed = false

    let cleanup () =
        if not disposed then
            disposed <- true
            try proc.StandardInput.Close() with _ -> ()
            if not proc.HasExited then
                try proc.Kill() with _ -> ()
            proc.Dispose()

    do
        AppDomain.CurrentDomain.ProcessExit.Add(fun _ -> cleanup())
        Console.CancelKeyPress.Add(fun e ->
            e.Cancel <- true
            cleanup())

    interface IAgent with
        member _.Send(prompt, onEvent) =
            sendToProcess proc.StandardInput proc.StandardOutput prompt onEvent

    interface IDisposable with
        member _.Dispose() = cleanup()

// ---------------------------------------------------------------------------
// DockerAgent — runs claude CLI in a Docker container
// ---------------------------------------------------------------------------

type DockerAgent(config: AgentConfig) =
    let containerName =
        let id = Guid.NewGuid().ToString("N").Substring(0, 8)
        $"loop-agent-{id}"

    let image =
        config.DockerImage |> Option.defaultValue "claude-agent-authed"

    let proc =
        let psi = ProcessStartInfo("docker")
        psi.ArgumentList.Add("run")
        psi.ArgumentList.Add("-i")
        psi.ArgumentList.Add("--rm")
        psi.ArgumentList.Add("--name")
        psi.ArgumentList.Add(containerName)
        if config.AllowedTools <> [] then
            psi.ArgumentList.Add("-v")
            psi.ArgumentList.Add($"{config.WorkingDirectory}:/workspace")
            psi.ArgumentList.Add("-w")
            psi.ArgumentList.Add("/workspace")
            match config.SharedFolder with
            | Some folder ->
                psi.ArgumentList.Add("-v")
                psi.ArgumentList.Add($"{folder}:/shared")
            | None -> ()
        let claudeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude")
        if Directory.Exists(claudeDir) then
            psi.ArgumentList.Add("-v")
            psi.ArgumentList.Add($"{claudeDir}:/root/.claude:ro")
        psi.ArgumentList.Add(image)
        for arg in buildClaudeArgs config do
            psi.ArgumentList.Add(arg)
        psi.RedirectStandardInput <- true
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        Process.Start(psi)

    let mutable disposed = false

    let cleanup () =
        if not disposed then
            disposed <- true
            try proc.StandardInput.Close() with _ -> ()
            try
                let kill = ProcessStartInfo("docker")
                kill.ArgumentList.Add("kill")
                kill.ArgumentList.Add(containerName)
                kill.CreateNoWindow <- true
                kill.UseShellExecute <- false
                kill.RedirectStandardOutput <- true
                kill.RedirectStandardError <- true
                use p = Process.Start(kill)
                p.WaitForExit(5000) |> ignore
            with _ -> ()
            proc.Dispose()

    do
        AppDomain.CurrentDomain.ProcessExit.Add(fun _ -> cleanup())
        Console.CancelKeyPress.Add(fun e ->
            e.Cancel <- true
            cleanup())

    interface IAgent with
        member _.Send(prompt, onEvent) =
            sendToProcess proc.StandardInput proc.StandardOutput prompt onEvent

    interface IDisposable with
        member _.Dispose() = cleanup()

// ---------------------------------------------------------------------------
// Factory
// ---------------------------------------------------------------------------

let createAgent (config: AgentConfig) : IAgent =
    match config.DockerImage with
    | Some _ -> new DockerAgent(config)
    | None -> new ProcessAgent(config)
