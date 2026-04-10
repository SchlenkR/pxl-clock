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

let renderPrompt (name: string) (vars: (string * string) list) =
    let mutable text = loadPrompt name
    for key, value in vars do
        text <- text.Replace("{{" + key + "}}", value)
    text

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
    | RunCraftsman
    | RunImplementor
    | Done of reason: string

let labelTriagePassed = "pixogram-triage-passed"
let labelTriageFailed = "pixogram-triage-failed"
let labelApproved = "pixogram-approved"
let labelPixogramIdea = "pixogram-idea"

type SafetyResult =
    | Passed
    | Failed of reason: string

let runSafetyCheck (issue: GitHub.Issue) : SafetyResult =
    printfn "  Safety check..."
    let prompt =
        renderPrompt "safety-check.md"
            [ "title", issue.Title
              "author", issue.Author
              "body", issue.Body ]
    match askAI Backends.safetyCheck prompt with
    | Error err ->
        printfn $"  ✗ Safety check failed: {err}"
        Failed $"AI error: {err}"
    | Ok response ->
        match findLastLineMatching "TRIAGE-" response with
        | Some line ->
            printfn $"  → {line}"
            if line.StartsWith "TRIAGE-PASSED" then Passed
            elif line.StartsWith "TRIAGE-FAILED" then Failed (line.Replace("TRIAGE-FAILED:", "").Trim())
            else Failed $"Unexpected response: {line}"
        | None ->
            printfn $"  ✗ No TRIAGE- line found in response"
            Failed "Could not parse safety check response"

let extractIterationCount (issueBody: string) =
    let prompt = renderPrompt "iteration-count.md" [ "default_iterations", string defaultIterations; "description", issueBody ]
    printfn "  Extracting iteration count..."
    match askAI Backends.triage prompt with
    | Error err ->
        printfn $"  ✗ Iteration extraction failed: {err}, defaulting to {defaultIterations}"
        defaultIterations
    | Ok response ->
        let trimmed = response.Trim()
        match System.Int32.TryParse trimmed with
        | true, n when n >= 1 ->
            printfn $"  → {n} iterations requested"
            n
        | _ ->
            printfn $"  ✗ Could not parse '{trimmed}', defaulting to {defaultIterations}"
            defaultIterations

let determineNextAction (maxIterations: int) (author: string) (conversation: string) =
    let fullPrompt =
        renderPrompt "triage.md"
            [ "admin", adminUser
              "author", author
              "max_iterations", string maxIterations
              "conversation", conversation ]

    printfn "  Triage..."
    match askAI Backends.triage fullPrompt with
    | Error err ->
        printfn $"  ✗ Triage failed: {err}"
        Done $"Triage error: {err}"
    | Ok response ->
        let keywords = [| "VISIONARY"; "MAVERICK"; "CRAFTSMAN"; "IMPLEMENTOR"; "DONE" |]
        let line =
            keywords
            |> Array.tryPick (fun kw -> findLastLineMatching kw response)
            |> Option.defaultValue ""
        printfn $"  → {line}"

        if line.StartsWith "VISIONARY" then RunVisionary
        elif line.StartsWith "MAVERICK" then RunMaverick
        elif line.StartsWith "CRAFTSMAN" then RunCraftsman
        elif line.StartsWith "IMPLEMENTOR" then RunImplementor
        elif line.StartsWith "DONE" then Done (line.Replace("DONE:", "").Trim())
        else RunVisionary

// ---------------------------------------------------------------------------
// Agent calls: backend + prompt + conversation → response string
// ---------------------------------------------------------------------------

let callAgent (backend: SelectedBackend) (promptFile: string) (conversation: string) : Result<string, string> =
    let prompt = renderPrompt promptFile [ "conversation", conversation ]
    askAI backend prompt
