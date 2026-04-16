module AiBase.CopilotSdkAgent

open System
open System.Text
open System.Threading
open AiBase.Agent
open GitHub.Copilot.SDK

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

type CopilotSdkConfig =
    {
        Model: string
        Effort: Effort
        AvailableTools: string list
    }

let defaultCopilotSdkConfig =
    {
        Model = "claude-sonnet-4.6"
        Effort = Medium
        AvailableTools = []
    }

// ---------------------------------------------------------------------------
// CopilotSdkAgent — uses GitHub.Copilot.SDK NuGet package
// ---------------------------------------------------------------------------

type CopilotSdkAgent(config: CopilotSdkConfig) =
    let mutable disposed = false
    let log msg = eprintfn $"    [copilot-sdk] {msg}"

    let effortStr =
        match config.Effort with
        | Low -> "low" | Medium -> "medium" | High -> "high" | Max -> "max"

    // Create and start the client
    let client =
        let opts = CopilotClientOptions()
        opts.AutoStart <- true
        opts.UseStdio <- true
        match Environment.GetEnvironmentVariable "COPILOT_GITHUB_TOKEN" with
        | null | "" -> ()
        | token ->
            opts.GitHubToken <- token
            log "Using COPILOT_GITHUB_TOKEN for auth"
        log $"Creating client (model: {config.Model}, effort: {effortStr})..."
        new CopilotClient(opts)

    let mutable session: CopilotSession option = None

    let ensureSession () =
        async {
            match session with
            | Some s -> return s
            | None ->
                log "Starting client..."
                do! client.StartAsync(CancellationToken.None) |> Async.AwaitTask

                log "Creating session..."
                let sessionConfig = SessionConfig()
                sessionConfig.Model <- config.Model
                sessionConfig.ReasoningEffort <- effortStr
                sessionConfig.Streaming <- true
                sessionConfig.OnPermissionRequest <- PermissionHandler.ApproveAll

                if config.AvailableTools <> [] then
                    sessionConfig.AvailableTools <- System.Collections.Generic.List<string>(config.AvailableTools)

                let! s = client.CreateSessionAsync(sessionConfig, CancellationToken.None) |> Async.AwaitTask
                log $"Session created: {s.SessionId}"
                session <- Some s
                return s
        }

    interface IAgent with
        member _.SendChat(messages, onEvent) =
            async {
                let! s = ensureSession()
                let prompt = ChatMessage.formatAsText messages
                log $"Sending prompt ({prompt.Length} chars)..."

                let fullResponse = StringBuilder()
                let tcs = System.Threading.Tasks.TaskCompletionSource<unit>()

                // Register event handler for streaming
                use _ = s.On(fun evt ->
                    match evt with
                    | :? AssistantMessageDeltaEvent as e ->
                        let delta = e.Data.DeltaContent
                        if not (String.IsNullOrEmpty delta) then
                            fullResponse.Append(delta) |> ignore
                            onEvent (Text delta)

                    | :? AssistantReasoningDeltaEvent as e ->
                        let delta = e.Data.DeltaContent
                        if not (String.IsNullOrEmpty delta) then
                            onEvent (Thinking delta)

                    | :? ExternalToolRequestedEvent as e ->
                        let title =
                            match e.Data with
                            | null -> "?"
                            | d -> d.GetType().Name
                        onEvent (ToolUse(title, ""))

                    | :? ExternalToolCompletedEvent as _e ->
                        onEvent (ToolResult "completed")

                    | :? SessionErrorEvent as e ->
                        let msg =
                            if isNull e.Data then "Unknown error"
                            else $"{e.Data.ErrorType}: {e.Data.Message} (HTTP {e.Data.StatusCode})"
                        log $"ERROR: {msg}"
                        onEvent (Error msg)
                        tcs.TrySetException(exn msg) |> ignore

                    | :? AssistantTurnEndEvent ->
                        tcs.TrySetResult() |> ignore

                    | _ -> ()
                )

                let msgOpts = MessageOptions()
                msgOpts.Prompt <- prompt

                let! _msgId = s.SendAsync(msgOpts, CancellationToken.None) |> Async.AwaitTask

                // Wait for turn to complete
                do! tcs.Task |> Async.AwaitTask

                let result = fullResponse.ToString()
                log $"Response received ({result.Length} chars)"
                onEvent (Result result)
                return result
            }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                log "Disposing..."
                match session with
                | Some s ->
                    try s.DisposeAsync().AsTask().Wait(5000) |> ignore with _ -> ()
                | None -> ()
                try client.DisposeAsync().AsTask().Wait(5000) |> ignore with _ -> ()
