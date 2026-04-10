module AiBase.AgentSelection

open System
open AiBase.DotEnv
open AiBase.Agent
open AiBase.OllamaAgent
open AiBase.CopilotAgent
open AiBase.CopilotSdkAgent
open AiBase.AnthropicAgent
open Spectre.Console

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type SelectedBackend =
    | Docker of image: string * model: Model * effort: Effort
    | Copilot of model: string * effort: Effort
    | Ollama of baseUrl: string * model: string
    | Anthropic of model: string

type BackendOptions =
    {
        DockerImage: string
        ClaudeModel: Model
        ClaudeEffort: Effort
        OllamaBaseUrl: string
        OllamaDefaultModel: string
    }

let defaultBackendOptions =
    {
        DockerImage = "claude-agent-authed"
        ClaudeModel = Sonnet
        ClaudeEffort = Low
        OllamaBaseUrl = "http://localhost:11434"
        OllamaDefaultModel = "gemma4:latest"
    }

let backendOptions () =
    load()
    let ollamaBaseUrl =
        Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")
        |> Option.ofObj
        |> Option.defaultValue defaultBackendOptions.OllamaBaseUrl
    { defaultBackendOptions with
        OllamaBaseUrl = ollamaBaseUrl }

// ---------------------------------------------------------------------------
// Selection
// ---------------------------------------------------------------------------

let selectBackend (label: string) (options: BackendOptions) : SelectedBackend =
    let prompt = SelectionPrompt<string>()
    prompt.Title <- $"Choose backend for [bold]{label}[/]:"
    prompt.AddChoices([ "Copilot CLI"; "Claude (Docker)"; "Ollama" ]) |> ignore
    let choice = AnsiConsole.Prompt(prompt)

    match choice with
    | "Copilot CLI" ->
        AnsiConsole.MarkupLine("[grey]Querying available models...[/]")
        let models = AiBase.CopilotAgent.listModels()
        let modelChoices =
            if models.IsEmpty then [ "claude-sonnet-4.6"; "claude-opus-4.6"; "gpt-4o" ]
            else models
        let modelPrompt = SelectionPrompt<string>()
        modelPrompt.Title <- $"Choose model for [bold]{label}[/]:"
        modelPrompt.AddChoices(modelChoices) |> ignore
        let model = AnsiConsole.Prompt(modelPrompt)

        let effortPrompt = SelectionPrompt<string>()
        effortPrompt.Title <- $"Choose thinking effort for [bold]{label}[/]:"
        effortPrompt.AddChoices([ "Low"; "Medium"; "High" ]) |> ignore
        let selectedEffort = AnsiConsole.Prompt(effortPrompt)
        let effort =
            match selectedEffort with
            | "Low" -> Low
            | "High" -> High
            | _ -> Medium

        Copilot(model, effort)
    | "Ollama" ->
        let models = OllamaAgent.listModels options.OllamaBaseUrl |> Async.RunSynchronously
        let running = OllamaAgent.listRunningModels options.OllamaBaseUrl |> Async.RunSynchronously |> Set.ofList
        let displayNames =
            models
            |> List.sortBy (fun m ->
                let isRunning = running.Contains(m)
                let isDefault = m = options.OllamaDefaultModel
                match isRunning, isDefault with
                | true, _ -> 0
                | _, true -> 1
                | _ -> 2)
            |> List.map (fun m ->
                if running.Contains(m) then $"{m} (running)" else m)
        let modelPrompt = SelectionPrompt<string>()
        modelPrompt.Title <- $"Choose Ollama model for [bold]{label}[/]:"
        modelPrompt.AddChoices(displayNames) |> ignore
        let selected = AnsiConsole.Prompt(modelPrompt)
        let model = selected.Replace(" (running)", "")
        Ollama(options.OllamaBaseUrl, model)
    | _ ->
        let modelPrompt = SelectionPrompt<string>()
        modelPrompt.Title <- $"Choose Claude model for [bold]{label}[/]:"
        modelPrompt.AddChoices([ "Opus"; "Sonnet"; "Haiku" ]) |> ignore
        let selectedModel = AnsiConsole.Prompt(modelPrompt)
        let model =
            match selectedModel with
            | "Opus" -> Opus
            | "Haiku" -> Haiku
            | _ -> Sonnet

        let effortPrompt = SelectionPrompt<string>()
        effortPrompt.Title <- $"Choose thinking effort for [bold]{label}[/]:"
        effortPrompt.AddChoices([ "Low"; "Medium"; "High"; "Max" ]) |> ignore
        let selectedEffort = AnsiConsole.Prompt(effortPrompt)
        let effort =
            match selectedEffort with
            | "Low" -> Low
            | "High" -> High
            | "Max" -> Max
            | _ -> Medium

        Docker(options.DockerImage, model, effort)

// ---------------------------------------------------------------------------
// Factory
// ---------------------------------------------------------------------------

let backendDisplayName (backend: SelectedBackend) =
    match backend with
    | Docker(_, model, effort) ->
        let m = match model with Opus -> "Opus" | Sonnet -> "Sonnet" | Haiku -> "Haiku" | Custom s -> s
        let e = match effort with Low -> "Low" | Medium -> "Medium" | High -> "High" | Max -> "Max"
        $"Claude {m} ({e})"
    | Copilot(model, effort) ->
        let e = match effort with Low -> "Low" | Medium -> "Medium" | High -> "High" | Max -> "Max"
        $"Copilot {model} ({e})"
    | Ollama(_, model) -> $"Ollama {model}"
    | Anthropic model -> $"Anthropic {model}"

let agentFactory (backend: SelectedBackend) (systemPrompt: string option) (sharedFolder: string option) (allowedTools: string list) : IAgent =
    match backend with
    | Docker(image, model, effort) ->
        createAgent
            { defaultConfig with
                Model = model
                Effort = effort
                DockerImage = Some image
                SharedFolder = sharedFolder
                SystemPrompt = systemPrompt
                AllowedTools = allowedTools }
    | Copilot(model, effort) ->
        new CopilotSdkAgent(
            { defaultCopilotSdkConfig with
                Model = model
                Effort = effort
                SystemPrompt = systemPrompt
                AvailableTools =
                    if allowedTools.IsEmpty then defaultCopilotSdkConfig.AvailableTools
                    else allowedTools })
    | Ollama(baseUrl, model) ->
        new OllamaAgent(
            {
                BaseUrl = baseUrl
                Model = model
                SystemPrompt = systemPrompt
            })
    | Anthropic model ->
        let apiKey =
            System.Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
            |> Option.ofObj
            |> Option.defaultValue ""
        new AnthropicAgent(
            {
                ApiKey = apiKey
                Model = model
                MaxTokens = 8192
                SystemPrompt = systemPrompt
            })
