open System
open Spectre.Console
open PixogramRequests.GitHub
open PixogramRequests.Config
open PixogramRequests.Conversation

AiBase.DotEnv.load()

// ---------------------------------------------------------------------------
// Environment → PipelineConfig
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

let private splitList (value: string) =
    value.Split([| ','; ';'; ' ' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.toList

let buildPipelineConfig (models: ConfigSet) : PipelineConfig =
    {
        Models = models
        Maintainers = splitList (envRequired "MAINTAINERS")
        TrustedAuthors = splitList (envRequired "TRUSTED_AUTHORS")
        DefaultIterations = envRequiredInt "DEFAULT_ITERATIONS"
        MaxIterationsCap = envRequiredInt "MAX_ITERATIONS_CAP"
        MaxImplementorRetries = envRequiredInt "MAX_IMPLEMENTOR_RETRIES"
        AiTimeoutMs = envRequiredInt "AI_TIMEOUT_MS"
        GifDurationSeconds = envRequiredInt "GIF_DURATION_SECONDS"
        GifScale = envRequiredInt "GIF_SCALE"
    }

let configFromEnv () =
    let name = envRequired "CONFIG_SET"
    let models = resolveConfigSet name
    let config = buildPipelineConfig models
    printfn $"  Config: {models.Name}"
    config

// ---------------------------------------------------------------------------
// Interactive helpers
// ---------------------------------------------------------------------------

let selectIssue (issues: Issue list) =
    let prompt =
        SelectionPrompt<string>()
            .Title("Select an issue:")
            .AddChoices(
                [ for issue in issues -> $"#{issue.Number}  {issue.Title}"
                  yield "[grey]Back[/]" ])
    let choice = AnsiConsole.Prompt prompt
    if choice.Contains "Back" then None
    else
        let number = choice.Split(' ').[0].TrimStart('#') |> int
        issues |> List.tryFind (fun i -> i.Number = number)

let withIssue (issues: Issue list) (action: Issue -> unit) =
    match selectIssue issues with
    | None -> ()
    | Some issue ->
        let full = fetchIssueWithComments issue
        AnsiConsole.MarkupLine $"[bold]=== #{full.Number}: {full.Title} ===[/]"
        action full

let doTriage (config: PipelineConfig) (issues: Issue list) =
    withIssue issues (fun issue ->
        PixogramRequests.Workflow.triageOnly config issue
        AnsiConsole.WriteLine())

let doWorkflow (config: PipelineConfig) (issues: Issue list) =
    withIssue issues (PixogramRequests.Workflow.run config)

let doShowConversation (config: PipelineConfig) (issues: Issue list) =
    withIssue issues (fun issue ->
        let viewPrompt =
            SelectionPrompt<string>()
                .Title("Select conversation view:")
                .AddChoices([ "Full"; "Implementor" ])
        let choice = AnsiConsole.Prompt viewPrompt
        let view =
            match choice with
            | "Implementor" -> ConversationView.Implementor
            | _ -> ConversationView.Full
        let xml = buildConversation config view None issue
        AnsiConsole.WriteLine()
        printfn "%s" xml
        AnsiConsole.WriteLine())

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

let args = Environment.GetCommandLineArgs() |> Array.skip 1

match args with
| [| "workflow"; issueNum |] ->
    let config = configFromEnv ()
    let n = int issueNum
    printfn $"Running workflow on issue #{n}..."
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None -> printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        PixogramRequests.Workflow.run config full

| [| "triage"; issueNum |] ->
    let config = configFromEnv ()
    let n = int issueNum
    printfn $"Triaging issue #{n}..."
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None -> printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        PixogramRequests.Workflow.triageOnly config full

| [| "conversation"; issueNum |]
| [| "conversation"; issueNum; "full" |] ->
    let config = configFromEnv ()
    let n = int issueNum
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None -> printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        printfn "%s" (buildConversation config ConversationView.Full None full)

| [| "conversation"; issueNum; "implementor" |] ->
    let config = configFromEnv ()
    let n = int issueNum
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None -> printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        printfn "%s" (buildConversation config ConversationView.Implementor None full)

| [| "dispatch" |] ->
    let config = configFromEnv ()
    let issues = PixogramRequests.Workflow.dispatch config
    if issues.IsEmpty then
        printfn "No issues need attention."
        printfn "[]"
    else
        printfn $"{issues.Length} issue(s) need attention."
        // JSON output for GitHub Actions matrix
        let json =
            issues
            |> List.map (fun i -> $"{{\"number\":{i.Number}}}")
            |> String.concat ","
        printfn $"[{json}]"

| [| "dispatch"; issueNum |] ->
    let config = configFromEnv ()
    let n = int issueNum
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None ->
        printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        if PixogramRequests.Workflow.needsAttention config full then
            printfn $"Issue #{n} needs attention."
        else
            printfn $"Issue #{n} does not need attention."

| [| "triage-all" |] ->
    let config = configFromEnv ()
    printfn $"Scanning for untriaged issues in {owner}/{repoName}..."
    let issues = listUntriagedIssues ()
    if issues.IsEmpty then
        printfn "No untriaged issues found."
    else
        printfn $"Found {issues.Length} untriaged issue(s)."
        for issue in issues do
            printfn $"\n  === #{issue.Number}: {issue.Title} ==="
            let full = fetchIssueWithComments issue
            PixogramRequests.Workflow.triageOnly config full

| [| "dispatch-and-run" |] ->
    let config = configFromEnv ()
    printfn $"Dispatching issues in {owner}/{repoName}..."
    PixogramRequests.Workflow.dispatchAndRun config

| [| "workflow-all" |] ->
    let config = configFromEnv ()
    printfn $"Scanning for open issues in {owner}/{repoName}..."
    let issues = listEligibleIssues ()
    if issues.IsEmpty then
        printfn "No open issues found."
    else
        printfn $"Found {issues.Length} open issue(s)."
        for issue in issues do
            printfn $"\n  === #{issue.Number}: {issue.Title} ==="
            let full = fetchIssueWithComments issue
            PixogramRequests.Workflow.run config full

| _ ->
    // Interactive mode
    AnsiConsole.MarkupLine "[bold blue]Pixogram Requests[/]"
    AnsiConsole.WriteLine()

    // Config set selection
    let configPrompt =
        SelectionPrompt<string>()
            .Title("Select config set:")
            .AddChoices([ for cs in configSets -> cs.Name ])
    let selectedConfig = AnsiConsole.Prompt configPrompt
    let models = configSets |> List.find (fun cs -> cs.Name = selectedConfig)
    let config = buildPipelineConfig models
    printfn $"  Config: {models.Name}"
    AnsiConsole.WriteLine()

    let issues =
        AnsiConsole.Status()
            .Start("Fetching issues...", fun _ -> listEligibleIssues ())

    if issues.IsEmpty then
        AnsiConsole.MarkupLine "[yellow]No open issues found.[/]"
    else
        let mutable running = true
        while running do
            let prompt =
                SelectionPrompt<string>()
                    .Title("What do you want to do?")
                    .AddChoices([ "Triage"; "Run Workflow"; "Show Conversation"; "[grey]Exit[/]" ])
            let choice = AnsiConsole.Prompt prompt

            match choice with
            | c when c.Contains "Exit" -> running <- false
            | "Triage" -> doTriage config issues
            | "Run Workflow" -> doWorkflow config issues
            | "Show Conversation" -> doShowConversation config issues
            | _ -> ()
