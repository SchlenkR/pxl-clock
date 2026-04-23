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
    let opus47 = "claude-opus-4-7"
    let sonnet46 = "claude-sonnet-4-6"
    let haiku45 = "claude-haiku-4-5"

module OllamaModels =
    let gemma4_26b = "gemma4:26b-a4b-it-q8_0"
    let gemma4_8b = "gemma4:latest"
    let qwen36_35b = "qwen3.6:35b-a3b-nvfp4"
    let qwen36_coding_mxfp8 = "qwen3.6:35b-a3b-coding-mxfp8"
    let qwen35_27b_q8 = "qwen3.5:27b-q8_0"
    let gptOss_120b = "gpt-oss:120b"
    let gptOss_20b = "gpt-oss:20b"
    let nemotronCascade2 = "nemotron-cascade-2:latest"
    let nemotron3Super = "nemotron-3-super:latest"
    // Feb 2026 addition
    let glm47Flash = "glm-4.7-flash:q8_0"                       // 30B/3B MoE, coding + agentic

module OpenRouterModels =
    let minimaxM25 = "minimax/minimax-m2.5"                     // 196k ctx, paid
    let minimaxM25Free = "minimax/minimax-m2.5:free"            // 196k ctx, free tier

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
// OpenRouter env helper.
// Reads OPENROUTER_API_KEY; falls back to OPENROUTER_PXL_APIKEY so the same
// variable name works locally (shell export) and in GitHub Actions secrets.
// ---------------------------------------------------------------------------

let private openRouterApiKey () : string =
    [ "OPENROUTER_API_KEY"; "OPENROUTER_PXL_APIKEY" ]
    |> List.tryPick (fun name ->
        match Environment.GetEnvironmentVariable name with
        | null | "" -> None
        | v -> Some v)
    |> Option.defaultValue ""

let private openRouterBackend (model: string) (effort: Effort) : SelectedBackend =
    OpenAI("https://openrouter.ai/api/v1", model, openRouterApiKey (), effort)

let private hasOpenRouterKey () =
    openRouterApiKey () <> ""

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
                SafetyCheck = Anthropic(AnthropicModels.sonnet46, Medium)
                Triage = Anthropic(AnthropicModels.sonnet46, Medium)
                DirectorVisionary = Anthropic(AnthropicModels.sonnet46, Medium)
                DirectorMaverick = Anthropic(AnthropicModels.sonnet46, Medium)
                Implementor = Anthropic(AnthropicModels.sonnet46, Medium)
                ImplementorFallback = None
                Compaction = Anthropic(AnthropicModels.haiku45, Low)
                ContextLengthTokens = 180_000
                CompactionThreshold = 0.8
            }

            {
                // Sonnet 4.6 across all roles with High budget-style extended thinking
                // (16k tokens). Direct counterpart to claude-opus-4.7 for A/B comparison.
                Name = "claude-sonnet-4.6-high"
                SafetyCheck = Anthropic(AnthropicModels.sonnet46, High)
                Triage = Anthropic(AnthropicModels.sonnet46, High)
                DirectorVisionary = Anthropic(AnthropicModels.sonnet46, High)
                DirectorMaverick = Anthropic(AnthropicModels.sonnet46, High)
                Implementor = Anthropic(AnthropicModels.sonnet46, High)
                ImplementorFallback = None
                Compaction = Anthropic(AnthropicModels.haiku45, Low)
                ContextLengthTokens = 180_000
                CompactionThreshold = 0.8
            }

            {
                // Claude Opus 4.7 across all roles with High extended-thinking budget
                // (16k tokens). Compaction uses Haiku 4.5 to save cost — the summary
                // task doesn't need deep reasoning.
                Name = "claude-opus-4.7"
                SafetyCheck = Anthropic(AnthropicModels.opus47, High)
                Triage = Anthropic(AnthropicModels.opus47, High)
                DirectorVisionary = Anthropic(AnthropicModels.opus47, High)
                DirectorMaverick = Anthropic(AnthropicModels.opus47, High)
                Implementor = Anthropic(AnthropicModels.opus47, High)
                ImplementorFallback = None
                Compaction = Anthropic(AnthropicModels.haiku45, Low)
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
                    // Same as qwen36-coding-mxfp8 but with Implementor thinking OFF.
                    // Tests whether the "THINK FIRST" prompt section alone (plaintext analysis)
                    // can replace internal <think> tokens — saving ~85% of Implementor gen time.
                    Name = "ollama1-qwen36-coding-nothink"
                    SafetyCheck = o1 OllamaModels.qwen36_35b
                    Triage = o1 OllamaModels.qwen36_35b
                    DirectorVisionary = o1 OllamaModels.qwen36_35b
                    DirectorMaverick = o1 OllamaModels.qwen36_35b
                    Implementor = ollamaBackend "OLLAMA1" OllamaModels.qwen36_coding_mxfp8 false
                    // If no-think produces a non-code response (e.g. imitates comment format from
                    // the conversation), retry same model with thinking ON.
                    ImplementorFallback = Some (ollamaBackend "OLLAMA1" OllamaModels.qwen36_coding_mxfp8 true)
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
                {
                    // OpenAI GPT-OSS 120B: 117B total / 5.1B active MoE, MXFP4, ~65 GB resident.
                    // Compaction on the smaller 20B sibling (13 GB) to keep the big model warm.
                    Name = "ollama1-gpt-oss-120b"
                    SafetyCheck = o1 OllamaModels.gptOss_120b
                    Triage = o1 OllamaModels.gptOss_120b
                    DirectorVisionary = o1 OllamaModels.gptOss_120b
                    DirectorMaverick = o1 OllamaModels.gptOss_120b
                    Implementor = o1 OllamaModels.gptOss_120b
                    ImplementorFallback = None
                    Compaction = o1 OllamaModels.gptOss_20b
                    ContextLengthTokens = 128_000
                    CompactionThreshold = 0.8
                }
                {
                    // NVIDIA Nemotron 3 Super: 120B total / 12B active MoE, q4_K_M GGUF, ~87 GB.
                    // No NVFP4 variant on Ollama (llama.cpp limitation).
                    Name = "ollama1-nemotron-3-super"
                    SafetyCheck = o1 OllamaModels.nemotron3Super
                    Triage = o1 OllamaModels.nemotron3Super
                    DirectorVisionary = o1 OllamaModels.nemotron3Super
                    DirectorMaverick = o1 OllamaModels.nemotron3Super
                    Implementor = o1 OllamaModels.nemotron3Super
                    ImplementorFallback = None
                    Compaction = o1 OllamaModels.nemotron3Super
                    ContextLengthTokens = 128_000
                    CompactionThreshold = 0.8
                }
                {
                    // NVIDIA Nemotron Cascade 2: 30B total / 3B active MoE, Q4_K_M, ~24 GB.
                    // 256K native context but we cap at 128K to match other sets.
                    Name = "ollama1-nemotron-cascade-2"
                    SafetyCheck = o1 OllamaModels.nemotronCascade2
                    Triage = o1 OllamaModels.nemotronCascade2
                    DirectorVisionary = o1 OllamaModels.nemotronCascade2
                    DirectorMaverick = o1 OllamaModels.nemotronCascade2
                    Implementor = o1 OllamaModels.nemotronCascade2
                    ImplementorFallback = None
                    Compaction = o1 OllamaModels.nemotronCascade2
                    ContextLengthTokens = 128_000
                    CompactionThreshold = 0.8
                }
                {
                    // Zhipu GLM-4.7-Flash: 30B total / 3B active MoE, Q8_0, ~32 GB.
                    // Strong coding + agentic (SWE-bench 59%). 198K native context.
                    Name = "ollama1-glm-4.7-flash"
                    SafetyCheck = o1 OllamaModels.glm47Flash
                    Triage = o1 OllamaModels.glm47Flash
                    DirectorVisionary = o1 OllamaModels.glm47Flash
                    DirectorMaverick = o1 OllamaModels.glm47Flash
                    Implementor = o1 OllamaModels.glm47Flash
                    ImplementorFallback = None
                    Compaction = o1 OllamaModels.glm47Flash
                    ContextLengthTokens = 128_000
                    CompactionThreshold = 0.8
                }
        ]

    // OpenRouter sets — only offered when an OpenRouter API key is present, so
    // the CLI's config listing stays clean on machines without OpenRouter creds.
    let openRouterSets =
        if hasOpenRouterKey () then
            let or_ = openRouterBackend
            [
                {
                    // MiniMax M2.5 via OpenRouter: 196K context, strong coding + reasoning.
                    Name = "openrouter-minimax-m2.5"
                    SafetyCheck = or_ OpenRouterModels.minimaxM25 Medium
                    Triage = or_ OpenRouterModels.minimaxM25 Medium
                    DirectorVisionary = or_ OpenRouterModels.minimaxM25 Medium
                    DirectorMaverick = or_ OpenRouterModels.minimaxM25 Medium
                    Implementor = or_ OpenRouterModels.minimaxM25 High
                    ImplementorFallback = None
                    Compaction = or_ OpenRouterModels.minimaxM25 Low
                    ContextLengthTokens = 196_000
                    CompactionThreshold = 0.8
                }
            ]
        else []

    staticSets @ ollamaSets @ openRouterSets

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
    // 1) Persist to RunLogger (workflow.log + raw.jsonl + step files).
    //    Safe to call when no run is active — it no-ops.
    RunLogger.logAgentEvent event

    // 2) Mirror to console for live feedback. Raw events are too noisy for
    //    the console — they go to disk only.
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
    | Metrics _ -> ()
    | RawRequest _ | RawEvent _ -> ()

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

type CallStats =
    {
        Metrics: CallMetrics option
        ThinkingChars: int
        TextChars: int
    }

let emptyCallStats = { Metrics = None; ThinkingChars = 0; TextChars = 0 }

let private doAskChat (backend: SelectedBackend) (timeoutMs: int) (messages: ChatMessage list) : Result<string * CallStats, string> =
    let name = backendDisplayName backend
    let totalChars = messages |> List.sumBy (fun m -> m.Content.Length)
    printfn $"    [askChat] Backend: {name}"
    printfn $"    [askChat] Messages: {messages.Length}, Total: {totalChars} chars, Timeout: {timeoutMs / 1000}s"
    // Persist the full outgoing request (messages + backend) to workflow.log + step request.json.
    // Called before SendChat so the log is complete even if the call crashes mid-flight.
    RunLogger.logRequest backend messages
    let metricsRef = ref None
    let thinkingChars = ref 0
    let textChars = ref 0
    let wrapped (e: AgentEvent) =
        match e with
        | Metrics m -> metricsRef.Value <- Some m
        | Thinking t -> thinkingChars.Value <- thinkingChars.Value + t.Length
        | Text t -> textChars.Value <- textChars.Value + t.Length
        | _ -> ()
        onEvent e
    try
        printfn $"    [askChat] Creating agent..."
        use agent = agentFactory backend
        printfn $"    [askChat] Agent ready, sending messages..."
        let work = agent.SendChat(messages, wrapped)
        let result =
            Async.RunSynchronously(work, timeout = timeoutMs)
        let trimmed = result.Trim()
        let stats = { Metrics = metricsRef.Value; ThinkingChars = thinkingChars.Value; TextChars = textChars.Value }
        // Tail: dump thinking + final response + metrics into workflow.log so the narrative
        // reads top-to-bottom: REQUEST → THINKING → RESPONSE → METRICS. Step-specific files
        // (thinking.txt / response.txt / raw.jsonl) already have full content for analysis.
        RunLogger.logThinkingTail ()
        RunLogger.logResponseTail trimmed
        RunLogger.logMetricsTail ()
        if String.IsNullOrWhiteSpace trimmed then
            printfn $"    [askChat] ERROR: empty response"
            Result.Error "AI returned empty response"
        else
            printfn $"    [askChat] OK: {trimmed.Length} chars"
            Ok (trimmed, stats)
    with
    | :? TimeoutException ->
        printfn $"    [askChat] ERROR: timed out after {timeoutMs / 1000}s"
        Result.Error $"AI call timed out after {timeoutMs / 1000}s"
    | ex ->
        printfn $"    [askChat] ERROR: {ex.Message}"
        Result.Error $"AI call failed: {ex.Message}"

/// Run an AI call inside a named RunLogger step. Every call to askChat(Ex) goes
/// through here so the log always has clear step boundaries with descriptive names.
let askChatEx (stepName: string) (backend: SelectedBackend) (timeoutMs: int) (messages: ChatMessage list) : Result<string * CallStats, string> =
    RunLogger.beginStep stepName
    try
        doAskChat backend timeoutMs messages
    finally
        RunLogger.endStep ()

let askChat (stepName: string) (backend: SelectedBackend) (timeoutMs: int) (messages: ChatMessage list) : Result<string, string> =
    askChatEx stepName backend timeoutMs messages |> Result.map fst
