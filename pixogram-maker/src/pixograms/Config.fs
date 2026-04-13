module PixogramRequests.Config

open System
open AiBase.Agent
open AiBase.AgentSelection

// ---------------------------------------------------------------------------
// Settings (all from environment variables, no fallbacks)
// ---------------------------------------------------------------------------

let private envRequired (name: string) =
    match Environment.GetEnvironmentVariable name with
    | null | "" -> failwith $"Required environment variable '{name}' is not set."
    | v -> v

let private envRequiredInt (name: string) =
    let v = envRequired name
    match Int32.TryParse v with
    | true, n -> n
    | _ -> failwith $"Environment variable '{name}' must be an integer, got '{v}'."

let maintainers =
    (envRequired "MAINTAINERS").Split([| ','; ';'; ' ' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.toList

let isMaintainer (user: string) =
    maintainers |> List.exists (fun m -> String.Equals(m, user, StringComparison.OrdinalIgnoreCase))

let trustedAuthors =
    (envRequired "TRUSTED_AUTHORS").Split([| ','; ';'; ' ' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.toList

let isTrustedAuthor (user: string) =
    trustedAuthors |> List.exists (fun m -> String.Equals(m, user, StringComparison.OrdinalIgnoreCase))

let defaultIterations = envRequiredInt "DEFAULT_ITERATIONS"
let maxImplementorRetries = envRequiredInt "MAX_IMPLEMENTOR_RETRIES"
let aiTimeoutMs = envRequiredInt "AI_TIMEOUT_MS"
let gifDurationSeconds = envRequiredInt "GIF_DURATION_SECONDS"
let gifScale = envRequiredInt "GIF_SCALE"

// ---------------------------------------------------------------------------
// Known Copilot models
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

// ---------------------------------------------------------------------------
// Backend configuration
// ---------------------------------------------------------------------------

module AnthropicModels =
    let opus46 = "claude-opus-4-6"
    let sonnet46 = "claude-sonnet-4-6"
    let haiku45 = "claude-haiku-4-5"

module Backends =
    let mutable safetyCheck = Anthropic AnthropicModels.sonnet46
    let mutable triage = Anthropic AnthropicModels.sonnet46
    let mutable directorVisionary = Anthropic AnthropicModels.sonnet46
    let mutable craftsman = Anthropic AnthropicModels.sonnet46
    let mutable directorMaverick = Anthropic AnthropicModels.sonnet46
    let mutable implementor = Anthropic AnthropicModels.sonnet46

type ConfigSet =
    {
        Name: string
        SafetyCheck: SelectedBackend
        Triage: SelectedBackend
        DirectorVisionary: SelectedBackend
        Craftsman: SelectedBackend
        DirectorMaverick: SelectedBackend
        Implementor: SelectedBackend
    }

let configSets =
    [
        {
            Name = "Claude (Anthropic API)"
            SafetyCheck = Anthropic AnthropicModels.sonnet46
            Triage = Anthropic AnthropicModels.sonnet46
            DirectorVisionary = Anthropic AnthropicModels.sonnet46
            Craftsman = Anthropic AnthropicModels.sonnet46
            DirectorMaverick = Anthropic AnthropicModels.sonnet46
            Implementor = Anthropic AnthropicModels.sonnet46
        }

        {
            Name = "Copilot (GPT 5.4)"
            SafetyCheck = Copilot(CopilotModels.gpt54, Medium)
            Triage = Copilot(CopilotModels.gpt54, Medium)
            DirectorVisionary = Copilot(CopilotModels.gpt54, Medium)
            Craftsman = Copilot(CopilotModels.gpt54, Medium)
            DirectorMaverick = Copilot(CopilotModels.gpt54, Medium)
            Implementor = Copilot(CopilotModels.gpt54, Medium)
        }
    ]

let applyConfigSet (cs: ConfigSet) =
    Backends.safetyCheck <- cs.SafetyCheck
    Backends.triage <- cs.Triage
    Backends.directorVisionary <- cs.DirectorVisionary
    Backends.craftsman <- cs.Craftsman
    Backends.directorMaverick <- cs.DirectorMaverick
    Backends.implementor <- cs.Implementor
    printfn $"  Config: {cs.Name}"

let applyConfigSetFromEnv () =
    let name = envRequired "CONFIG_SET"
    match configSets |> List.tryFind (fun cs -> cs.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) with
    | Some cs -> applyConfigSet cs
    | None ->
        let available = configSets |> List.map (fun cs -> cs.Name) |> String.concat ", "
        failwith $"Unknown CONFIG_SET '{name}'. Available: {available}"

// ---------------------------------------------------------------------------
// AI call helper
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

let sendToAgent (agent: IAgent) (prompt: string) : Result<string, string> =
    printfn $"    [agent] Sending {prompt.Length} chars..."
    try
        let work = agent.Send(prompt, onEvent)
        let result = Async.RunSynchronously(work, timeout = aiTimeoutMs)
        let trimmed = result.Trim()
        if String.IsNullOrWhiteSpace trimmed then
            printfn $"    [agent] ERROR: empty response"
            Result.Error "AI returned empty response"
        else
            printfn $"    [agent] OK: {trimmed.Length} chars"
            Ok trimmed
    with
    | :? TimeoutException ->
        printfn $"    [agent] ERROR: timed out after {aiTimeoutMs / 1000}s"
        Result.Error $"AI call timed out after {aiTimeoutMs / 1000}s"
    | ex ->
        printfn $"    [agent] ERROR: {ex.Message}"
        Result.Error $"AI call failed: {ex.Message}"

let askAI (backend: SelectedBackend) (prompt: string) : Result<string, string> =
    let name = backendDisplayName backend
    printfn $"    [askAI] Backend: {name}"
    printfn $"    [askAI] Prompt: {prompt.Length} chars, Timeout: {aiTimeoutMs / 1000}s"
    try
        printfn $"    [askAI] Creating agent..."
        use agent = agentFactory backend None None []
        printfn $"    [askAI] Agent ready, sending prompt..."
        let work = agent.Send(prompt, onEvent)
        let result =
            Async.RunSynchronously(work, timeout = aiTimeoutMs)
        let trimmed = result.Trim()
        if String.IsNullOrWhiteSpace trimmed then
            printfn $"    [askAI] ERROR: empty response"
            Result.Error "AI returned empty response"
        else
            printfn $"    [askAI] OK: {trimmed.Length} chars"
            Ok trimmed
    with
    | :? TimeoutException ->
        printfn $"    [askAI] ERROR: timed out after {aiTimeoutMs / 1000}s"
        Result.Error $"AI call timed out after {aiTimeoutMs / 1000}s"
    | ex ->
        printfn $"    [askAI] ERROR: {ex.Message}"
        Result.Error $"AI call failed: {ex.Message}"
