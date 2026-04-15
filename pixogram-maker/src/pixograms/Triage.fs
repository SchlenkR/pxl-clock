module PixogramRequests.Triage

open System.IO
open AiBase.AgentSelection
open PixogramRequests.Config

// ---------------------------------------------------------------------------
// Role protocol
// ---------------------------------------------------------------------------

[<RequireQualifiedAccess>]
type Role =
    | Director
    | Implementor
    | Admin

let roleTag (role: Role) =
    match role with
    | Role.Director -> "**[Director]**"
    | Role.Implementor -> "**[Implementor]**"
    | Role.Admin -> "**[Admin]**"

// ---------------------------------------------------------------------------
// Prompts
// ---------------------------------------------------------------------------

let projectDir = __SOURCE_DIRECTORY__

let loadPrompt name =
    File.ReadAllText(Path.Combine(projectDir, "prompts", name)).Trim()

let private conversationFormatBlock =
    lazy (loadPrompt "conversation-format.md")

let renderPrompt (name: string) (vars: (string * string) list) =
    let mutable text = loadPrompt name
    for key, value in vars do
        text <- text.Replace("{{" + key + "}}", value)
    // Insert conversation format description between conversation data and instructions
    text.Replace("---\n\n# Instructions", $"---\n\n{conversationFormatBlock.Value}\n\n---\n\n# Instructions")

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private findLastLineMatching (prefix: string) (response: string) =
    response.Split('\n')
    |> Array.rev
    |> Array.tryFind (fun l ->
        let trimmed = l.Trim().TrimStart('`').TrimEnd('`').Trim()
        trimmed.StartsWith prefix)
    |> Option.map (fun l -> l.Trim().TrimStart('`').TrimEnd('`').Trim())

// ---------------------------------------------------------------------------
// Triage: conversation string → next action
// ---------------------------------------------------------------------------

type NextAction =
    | RunVisionary
    | RunMaverick
    | RunImplementor
    | Done of reason: string

let labelTriagePassed = "pixogram-triage-passed"
let labelApproved = "pixogram-approved"
let labelPixogramIdea = "pixogram-idea"
let labelIgnore = "pixogram-ignore"

[<RequireQualifiedAccess>]
type SafetyResult =
    | Passed
    | Failed of reason: string
    | Error of reason: string

let runSafetyCheck (config: PipelineConfig) (issue: GitHub.Issue) : SafetyResult =
    printfn "  Safety check..."
    let prompt =
        renderPrompt "safety-check.md"
            [ "title", issue.Title
              "author", issue.Author
              "body", issue.Body ]
    match askAI config.Models.SafetyCheck config.AiTimeoutMs prompt with
    | Result.Error err ->
        printfn $"  ✗ Safety check AI error: {err}"
        SafetyResult.Error $"AI error: {err}"
    | Ok response ->
        match findLastLineMatching "TRIAGE-" response with
        | Some line ->
            printfn $"  → {line}"
            if line.StartsWith "TRIAGE-PASSED" then SafetyResult.Passed
            elif line.StartsWith "TRIAGE-FAILED" then SafetyResult.Failed (line.Replace("TRIAGE-FAILED:", "").Trim())
            else SafetyResult.Failed $"Unexpected response: {line}"
        | None ->
            printfn $"  ✗ No TRIAGE- line found in response"
            SafetyResult.Failed "Could not parse safety check response"

let runCommentSafetyCheck (config: PipelineConfig) (context: string) (author: string) (commentBody: string) : SafetyResult =
    printfn "  Comment safety check..."
    let prompt =
        renderPrompt "comment-safety-check.md"
            [ "context", context
              "author", author
              "body", commentBody ]
    match askAI config.Models.SafetyCheck config.AiTimeoutMs prompt with
    | Result.Error err ->
        printfn $"  ✗ Comment safety check AI error: {err}"
        SafetyResult.Error $"AI error: {err}"
    | Ok response ->
        match findLastLineMatching "TRIAGE-" response with
        | Some line ->
            printfn $"  → {line}"
            if line.StartsWith "TRIAGE-PASSED" then SafetyResult.Passed
            elif line.StartsWith "TRIAGE-FAILED" then SafetyResult.Failed (line.Replace("TRIAGE-FAILED:", "").Trim())
            else SafetyResult.Failed $"Unexpected response: {line}"
        | None ->
            printfn $"  ✗ No TRIAGE- line found in response"
            SafetyResult.Failed "Could not parse comment safety check response"

let extractIterationCount (config: PipelineConfig) (issueBody: string) =
    let prompt = renderPrompt "iteration-count.md" [ "default_iterations", string config.DefaultIterations; "description", issueBody ]
    printfn "  Extracting iteration count..."
    match askAI config.Models.Triage config.AiTimeoutMs prompt with
    | Error err ->
        printfn $"  ✗ Iteration extraction failed: {err}, defaulting to {config.DefaultIterations}"
        config.DefaultIterations
    | Ok response ->
        let trimmed = response.Trim()
        match System.Int32.TryParse trimmed with
        | true, n when n >= 1 ->
            let capped = min n config.MaxIterationsCap
            if capped < n then
                printfn $"  → {n} iterations requested, capped to {capped} (MAX_ITERATIONS_CAP)"
            else
                printfn $"  → {capped} iterations requested"
            capped
        | _ ->
            printfn $"  ✗ Could not parse '{trimmed}', defaulting to {config.DefaultIterations}"
            config.DefaultIterations

let determineNextAction (config: PipelineConfig) (maxIterations: int) (author: string) (conversation: string) =
    let fullPrompt =
        renderPrompt "triage.md"
            [ "admin", String.concat ", " config.Maintainers
              "author", author
              "max_iterations", string maxIterations
              "conversation", conversation ]

    printfn "  Triage..."
    match askAI config.Models.Triage config.AiTimeoutMs fullPrompt with
    | Error err ->
        printfn $"  ✗ Triage failed: {err}"
        Done $"Triage error: {err}"
    | Ok response ->
        let keywords = [| "VISIONARY"; "MAVERICK"; "IMPLEMENTOR"; "DONE" |]
        let line =
            keywords
            |> Array.tryPick (fun kw -> findLastLineMatching kw response)
            |> Option.defaultValue ""
        printfn $"  → {line}"

        if line.StartsWith "VISIONARY" then RunVisionary
        elif line.StartsWith "MAVERICK" then RunMaverick
        elif line.StartsWith "CRAFTSMAN" then RunImplementor // legacy: treat as IMPLEMENTOR
        elif line.StartsWith "IMPLEMENTOR" then RunImplementor
        elif line.StartsWith "DONE" then Done (line.Replace("DONE:", "").Trim())
        else RunVisionary

// ---------------------------------------------------------------------------
// Agent calls: backend + prompt + conversation → response string
// ---------------------------------------------------------------------------

let callAgent (backend: SelectedBackend) (timeoutMs: int) (promptFile: string) (conversation: string) : Result<string, string> =
    let prompt = renderPrompt promptFile [ "conversation", conversation ]
    askAI backend timeoutMs prompt
