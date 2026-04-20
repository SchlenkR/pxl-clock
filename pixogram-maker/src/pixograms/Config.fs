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

module OllamaModels =
    let gemma4_26b = "gemma4:26b-a4b-it-q8_0"
    let gemma4_8b = "gemma4:latest"
    let qwen36_35b = "qwen3.6:35b-a3b-nvfp4"
    let qwen36_coding_mxfp8 = "qwen3.6:35b-a3b-coding-mxfp8"
    let qwen35_27b_q8 = "qwen3.5:27b-q8_0"

// ---------------------------------------------------------------------------
// Ollama env helper — reads OLLAMA{N}_URL / OLLAMA{N}_API_KEY
// ---------------------------------------------------------------------------

let private ollamaBackend (envPrefix: string) (model: string) (think: bool) : SelectedBackend =
    let url =
        Environment.GetEnvironmentVariable($"{envPrefix}_URL")
        |> Option.ofObj
        |> Option.defaultValue ""
    let apiKey =
        Environment.GetEnvironmentVariable($"{envPrefix}_API_KEY")
        |> Option.ofObj
        |> Option.bind (fun s -> if String.IsNullOrWhiteSpace s then None else Some s)
    Ollama(url, model, apiKey, think)

let private hasOllamaEnv (envPrefix: string) =
    Environment.GetEnvironmentVariable($"{envPrefix}_URL")
    |> String.IsNullOrEmpty
    |> not

// ---------------------------------------------------------------------------
// ConfigSet — which AI models to use for each pipeline role
// ---------------------------------------------------------------------------

type ConfigSet =
    {
        Name: string
        SafetyCheck: SelectedBackend
        Triage: SelectedBackend
        DirectorVisionary: SelectedBackend
        DirectorMaverick: SelectedBackend
        Implementor: SelectedBackend
        ImplementorFallback: SelectedBackend option
        Compaction: SelectedBackend
        ContextLengthTokens: int
        CompactionThreshold: float
    }

let configSets () =
    let staticSets =
        [
            {
                Name = "claude-sonnet-4.6/haiku-4.5"
                SafetyCheck = Anthropic AnthropicModels.sonnet46
                Triage = Anthropic AnthropicModels.sonnet46
                DirectorVisionary = Anthropic AnthropicModels.sonnet46
                DirectorMaverick = Anthropic AnthropicModels.sonnet46
                Implementor = Anthropic AnthropicModels.sonnet46
                ImplementorFallback = None
                Compaction = Anthropic AnthropicModels.haiku45
                ContextLengthTokens = 180_000
                CompactionThreshold = 0.8
            }

            {
                Name = "copilot-sonnet-4.6/haiku-4.5"
                SafetyCheck = Copilot(CopilotModels.sonnet46, Medium)
                Triage = Copilot(CopilotModels.sonnet46, Medium)
                DirectorVisionary = Copilot(CopilotModels.sonnet46, Medium)
                DirectorMaverick = Copilot(CopilotModels.sonnet46, Medium)
                Implementor = Copilot(CopilotModels.sonnet46, Medium)
                ImplementorFallback = None
                Compaction = Copilot(CopilotModels.haiku45, Low)
                ContextLengthTokens = 180_000
                CompactionThreshold = 0.8
            }

            {
                Name = "copilot-gpt-5.4/gpt-5.4-mini"
                SafetyCheck = Copilot(CopilotModels.gpt54, High)
                Triage = Copilot(CopilotModels.gpt54, Medium)
                DirectorVisionary = Copilot(CopilotModels.gpt54, Medium)
                DirectorMaverick = Copilot(CopilotModels.gpt54, Medium)
                Implementor = Copilot(CopilotModels.gpt54, Medium)
                ImplementorFallback = Some (Copilot(CopilotModels.sonnet46, Medium))
                Compaction = Copilot(CopilotModels.gpt54Mini, Low)
                ContextLengthTokens = 120_000
                CompactionThreshold = 0.8
            }
        ]

    let ollamaSets =
        [
            if hasOllamaEnv "OLLAMA1" then
                // Thinking enabled for all roles. Earlier no-think for Triage avoided
                // qwen3.x self-reinforcement loops but produced poor routing decisions
                // (e.g. picking Maverick when feedback was a concrete Implementor spec).
                let o1 model = ollamaBackend "OLLAMA1" model true
                {
                    Name = "ollama1-gemma4-26b"
                    SafetyCheck = o1 OllamaModels.gemma4_26b
                    Triage = o1 OllamaModels.gemma4_26b
                    DirectorVisionary = o1 OllamaModels.gemma4_26b
                    DirectorMaverick = o1 OllamaModels.gemma4_26b
                    Implementor = o1 OllamaModels.gemma4_26b
                    ImplementorFallback = None
                    Compaction = o1 OllamaModels.gemma4_8b
                    ContextLengthTokens = 128_000
                    CompactionThreshold = 0.8
                }
                {
                    Name = "ollama1-qwen36-35b"
                    SafetyCheck = o1 OllamaModels.qwen36_35b
                    Triage = o1 OllamaModels.qwen36_35b
                    DirectorVisionary = o1 OllamaModels.qwen36_35b
                    DirectorMaverick = o1 OllamaModels.qwen36_35b
                    Implementor = o1 OllamaModels.qwen36_35b
                    ImplementorFallback = None
                    Compaction = o1 OllamaModels.qwen36_35b
                    ContextLengthTokens = 128_000
                    CompactionThreshold = 0.8
                }
                {
                    // General model for non-code roles, coding fine-tune for Implementor.
                    // Both are 35B-A3B MoE so ~3B active → fast.
                    Name = "ollama1-qwen36-coding-mxfp8"
                    SafetyCheck = o1 OllamaModels.qwen36_35b
                    Triage = o1 OllamaModels.qwen36_35b
                    DirectorVisionary = o1 OllamaModels.qwen36_35b
                    DirectorMaverick = o1 OllamaModels.qwen36_35b
                    Implementor = o1 OllamaModels.qwen36_coding_mxfp8
                    ImplementorFallback = None
                    Compaction = o1 OllamaModels.qwen36_35b
                    ContextLengthTokens = 128_000
                    CompactionThreshold = 0.8
                }
                {
                    Name = "ollama1-qwen3.5-27b-q8"
                    SafetyCheck = o1 OllamaModels.qwen35_27b_q8
                    Triage = o1 OllamaModels.qwen35_27b_q8
                    DirectorVisionary = o1 OllamaModels.qwen35_27b_q8
                    DirectorMaverick = o1 OllamaModels.qwen35_27b_q8
                    Implementor = o1 OllamaModels.qwen35_27b_q8
                    ImplementorFallback = None
                    Compaction = o1 OllamaModels.qwen35_27b_q8
                    ContextLengthTokens = 128_000
                    CompactionThreshold = 0.8
                }
        ]

    staticSets @ ollamaSets

let resolveConfigSet (name: string) : ConfigSet =
    let sets = configSets ()
    match sets |> List.tryFind (fun cs -> cs.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) with
    | Some cs -> cs
    | None ->
        let available = sets |> List.map (fun cs -> cs.Name) |> String.concat ", "
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

// System prompt to prevent models (especially gpt-5.4) from attempting tool use.
// These agents have no tools available — without this instruction, some models
// produce empty responses because they try to call non-existent tools.
let noToolsPrompt =
    "You are a text-only AI. You have NO tools available. " +
    "You cannot read files, edit files, browse repositories, run commands, or access any external resources. " +
    "Output only text. Do not attempt to call tools or functions — they do not exist."

let createAgent (backend: SelectedBackend) : IAgent =
    agentFactory backend

let sendChat (agent: IAgent) (timeoutMs: int) (messages: ChatMessage list) : Result<string, string> =
    let totalChars = messages |> List.sumBy (fun m -> m.Content.Length)
    printfn $"    [agent] Sending {messages.Length} messages ({totalChars} chars)..."
    try
        let work = agent.SendChat(messages, onEvent)
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

let askChat (backend: SelectedBackend) (timeoutMs: int) (messages: ChatMessage list) : Result<string, string> =
    let name = backendDisplayName backend
    let totalChars = messages |> List.sumBy (fun m -> m.Content.Length)
    printfn $"    [askChat] Backend: {name}"
    printfn $"    [askChat] Messages: {messages.Length}, Total: {totalChars} chars, Timeout: {timeoutMs / 1000}s"
    try
        printfn $"    [askChat] Creating agent..."
        use agent = agentFactory backend
        printfn $"    [askChat] Agent ready, sending messages..."
        let work = agent.SendChat(messages, onEvent)
        let result =
            Async.RunSynchronously(work, timeout = timeoutMs)
        let trimmed = result.Trim()
        if String.IsNullOrWhiteSpace trimmed then
            printfn $"    [askChat] ERROR: empty response"
            Result.Error "AI returned empty response"
        else
            printfn $"    [askChat] OK: {trimmed.Length} chars"
            Ok trimmed
    with
    | :? TimeoutException ->
        printfn $"    [askChat] ERROR: timed out after {timeoutMs / 1000}s"
        Result.Error $"AI call timed out after {timeoutMs / 1000}s"
    | ex ->
        printfn $"    [askChat] ERROR: {ex.Message}"
        Result.Error $"AI call failed: {ex.Message}"
