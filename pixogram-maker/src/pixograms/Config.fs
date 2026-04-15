module PixogramRequests.Config

open System
open AiBase.Agent
open AiBase.AgentSelection

// ---------------------------------------------------------------------------
// Known model identifiers
// ---------------------------------------------------------------------------

module CopilotModels =
    // Claude
    let sonnet46 = "claude-sonnet-4.6"
    let sonnet45 = "claude-sonnet-4.5"
    let sonnet4 = "claude-sonnet-4"
    let opus46 = "claude-opus-4.6"
    let opus46Fast = "claude-opus-4.6-fast"
    let opus45 = "claude-opus-4.5"
    let haiku45 = "claude-haiku-4.5"
    // OpenAI
    let gpt54 = "gpt-5.4"
    let gpt53Codex = "gpt-5.3-codex"
    let gpt52Codex = "gpt-5.2-codex"
    let gpt52 = "gpt-5.2"
    let gpt51 = "gpt-5.1"
    let gpt54Mini = "gpt-5.4-mini"
    let gpt5Mini = "gpt-5-mini"
    let gpt41 = "gpt-4.1"

module AnthropicModels =
    let opus46 = "claude-opus-4-6"
    let sonnet46 = "claude-sonnet-4-6"
    let haiku45 = "claude-haiku-4-5"

// ---------------------------------------------------------------------------
// ConfigSet — which AI models to use for each pipeline role
// ---------------------------------------------------------------------------

type ConfigSet =
    {
        Name: string
        SafetyCheck: SelectedBackend
        Triage: SelectedBackend
        DirectorVisionary: SelectedBackend
        Craftsman: SelectedBackend
        DirectorMaverick: SelectedBackend
        Implementor: SelectedBackend
        Compaction: SelectedBackend
        ContextLengthTokens: int
        CompactionThreshold: float
    }

let configSets =
    [
        {
            Name = "claude-sonnet-4.6/haiku-4.5"
            SafetyCheck = Anthropic AnthropicModels.sonnet46
            Triage = Anthropic AnthropicModels.sonnet46
            DirectorVisionary = Anthropic AnthropicModels.sonnet46
            Craftsman = Anthropic AnthropicModels.sonnet46
            DirectorMaverick = Anthropic AnthropicModels.sonnet46
            Implementor = Anthropic AnthropicModels.sonnet46
            Compaction = Anthropic AnthropicModels.haiku45
            ContextLengthTokens = 180_000
            CompactionThreshold = 0.8
        }

        {
            Name = "copilot-sonnet-4.6/haiku-4.5"
            SafetyCheck = Copilot(CopilotModels.sonnet46, Medium)
            Triage = Copilot(CopilotModels.sonnet46, Medium)
            DirectorVisionary = Copilot(CopilotModels.sonnet46, Medium)
            Craftsman = Copilot(CopilotModels.sonnet46, Medium)
            DirectorMaverick = Copilot(CopilotModels.sonnet46, Medium)
            Implementor = Copilot(CopilotModels.sonnet46, Medium)
            Compaction = Copilot(CopilotModels.haiku45, Low)
            ContextLengthTokens = 180_000
            CompactionThreshold = 0.8
        }

        {
            Name = "copilot-gpt-5.4/gpt-5.4-mini"
            SafetyCheck = Copilot(CopilotModels.gpt54, High)
            Triage = Copilot(CopilotModels.gpt54, Medium)
            DirectorVisionary = Copilot(CopilotModels.gpt54, Medium)
            Craftsman = Copilot(CopilotModels.gpt54, Medium)
            DirectorMaverick = Copilot(CopilotModels.gpt54, Medium)
            Implementor = Copilot(CopilotModels.gpt54, Medium)
            Compaction = Copilot(CopilotModels.gpt54Mini, Low)
            ContextLengthTokens = 120_000
            CompactionThreshold = 0.8
        }
    ]

let resolveConfigSet (name: string) : ConfigSet =
    match configSets |> List.tryFind (fun cs -> cs.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) with
    | Some cs -> cs
    | None ->
        let available = configSets |> List.map (fun cs -> cs.Name) |> String.concat ", "
        failwith $"Unknown CONFIG_SET '{name}'. Available: {available}"

// ---------------------------------------------------------------------------
// PipelineConfig — everything a pipeline run needs, no globals
// ---------------------------------------------------------------------------

type PipelineConfig =
    {
        // AI backends (from ConfigSet)
        Models: ConfigSet
        // People
        Maintainers: string list
        TrustedAuthors: string list
        // Iteration limits
        DefaultIterations: int
        MaxIterationsCap: int
        MaxDirectorRetries: int
        MaxImplementorRetries: int
        // Timeouts & rendering
        AiTimeoutMs: int
        GifDurationSeconds: int
        GifScale: int
    }

let isMaintainer (config: PipelineConfig) (user: string) =
    config.Maintainers |> List.exists (fun m -> String.Equals(m, user, StringComparison.OrdinalIgnoreCase))

let isTrustedAuthor (config: PipelineConfig) (user: string) =
    config.TrustedAuthors |> List.exists (fun m -> String.Equals(m, user, StringComparison.OrdinalIgnoreCase))

// ---------------------------------------------------------------------------
// AI call helpers
// ---------------------------------------------------------------------------

let mutable private inThinking = false

let private endThinking () =
    if inThinking then
        Console.ResetColor()
        eprintfn ""
        inThinking <- false

let private onEvent (event: AgentEvent) =
    match event with
    | Thinking t ->
        if not inThinking then
            Console.ForegroundColor <- ConsoleColor.DarkGray
            eprintf "    [thinking] "
            inThinking <- true
        eprintf "%s" t
    | Text t ->
        endThinking()
        eprintf "%s" t
    | ToolUse(tool, input) ->
        endThinking()
        Console.ForegroundColor <- ConsoleColor.Magenta
        eprintfn "    [tool] %s: %s" tool input
        Console.ResetColor()
    | ToolResult output ->
        endThinking()
        let short = if output.Length > 200 then output.[..199] + "..." else output
        Console.ForegroundColor <- ConsoleColor.DarkGray
        eprintfn "    [result] %s" short
        Console.ResetColor()
    | Result _ ->
        endThinking()
        eprintfn ""
    | Error e ->
        endThinking()
        Console.ForegroundColor <- ConsoleColor.Red
        eprintfn "    [ERROR] %s" e
        Console.ResetColor()

let createAgent (backend: SelectedBackend) : IAgent =
    agentFactory backend None None []

let sendToAgent (agent: IAgent) (timeoutMs: int) (prompt: string) : Result<string, string> =
    printfn $"    [agent] Sending {prompt.Length} chars..."
    try
        let work = agent.Send(prompt, onEvent)
        let result = Async.RunSynchronously(work, timeout = timeoutMs)
        let trimmed = result.Trim()
        if String.IsNullOrWhiteSpace trimmed then
            printfn $"    [agent] ERROR: empty response"
            Result.Error "AI returned empty response"
        else
            printfn $"    [agent] OK: {trimmed.Length} chars"
            Ok trimmed
    with
    | :? TimeoutException ->
        printfn $"    [agent] ERROR: timed out after {timeoutMs / 1000}s"
        Result.Error $"AI call timed out after {timeoutMs / 1000}s"
    | ex ->
        printfn $"    [agent] ERROR: {ex.Message}"
        Result.Error $"AI call failed: {ex.Message}"

let askAI (backend: SelectedBackend) (timeoutMs: int) (prompt: string) : Result<string, string> =
    let name = backendDisplayName backend
    printfn $"    [askAI] Backend: {name}"
    printfn $"    [askAI] Prompt: {prompt.Length} chars, Timeout: {timeoutMs / 1000}s"
    try
        printfn $"    [askAI] Creating agent..."
        use agent = agentFactory backend None None []
        printfn $"    [askAI] Agent ready, sending prompt..."
        let work = agent.Send(prompt, onEvent)
        let result =
            Async.RunSynchronously(work, timeout = timeoutMs)
        let trimmed = result.Trim()
        if String.IsNullOrWhiteSpace trimmed then
            printfn $"    [askAI] ERROR: empty response"
            Result.Error "AI returned empty response"
        else
            printfn $"    [askAI] OK: {trimmed.Length} chars"
            Ok trimmed
    with
    | :? TimeoutException ->
        printfn $"    [askAI] ERROR: timed out after {timeoutMs / 1000}s"
        Result.Error $"AI call timed out after {timeoutMs / 1000}s"
    | ex ->
        printfn $"    [askAI] ERROR: {ex.Message}"
        Result.Error $"AI call failed: {ex.Message}"
