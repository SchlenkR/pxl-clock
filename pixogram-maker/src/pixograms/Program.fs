open System
open Spectre.Console
open PixogramRequests.GitHub
open PixogramRequests.Config
open PixogramRequests.Conversation

AiBase.DotEnv.load()

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

let doTriage (issues: Issue list) =
    withIssue issues (fun issue ->
        PixogramRequests.Workflow.triageOnly issue
        AnsiConsole.WriteLine())

let doWorkflow (issues: Issue list) =
    withIssue issues PixogramRequests.Workflow.run

let doShowConversation (issues: Issue list) =
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
        let xml = buildConversation view issue
        AnsiConsole.WriteLine()
        printfn "%s" xml
        AnsiConsole.WriteLine())

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

let args = Environment.GetCommandLineArgs() |> Array.skip 1

applyConfigSetFromEnv ()

match args with
| [| "workflow"; issueNum |] ->
    let n = int issueNum
    printfn $"Running workflow on issue #{n}..."
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None -> printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        PixogramRequests.Workflow.run full

| [| "triage"; issueNum |] ->
    let n = int issueNum
    printfn $"Triaging issue #{n}..."
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None -> printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        PixogramRequests.Workflow.triageOnly full

| [| "conversation"; issueNum |]
| [| "conversation"; issueNum; "full" |] ->
    let n = int issueNum
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None -> printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        printfn "%s" (buildConversation ConversationView.Full full)

| [| "conversation"; issueNum; "implementor" |] ->
    let n = int issueNum
    let issues = listEligibleIssues ()
    match issues |> List.tryFind (fun i -> i.Number = n) with
    | None -> printfn $"Issue #{n} not found."
    | Some issue ->
        let full = fetchIssueWithComments issue
        printfn "%s" (buildConversation ConversationView.Implementor full)

| [| "triage-all" |] ->
    printfn $"Scanning for untriaged issues in {owner}/{repoName}..."
    let issues = listUntriagedIssues ()
    if issues.IsEmpty then
        printfn "No untriaged issues found."
    else
        printfn $"Found {issues.Length} untriaged issue(s)."
        for issue in issues do
            printfn $"\n  === #{issue.Number}: {issue.Title} ==="
            let full = fetchIssueWithComments issue
            PixogramRequests.Workflow.triageOnly full

| [| "workflow-all" |] ->
    printfn $"Scanning for open issues in {owner}/{repoName}..."
    let issues = listEligibleIssues ()
    if issues.IsEmpty then
        printfn "No open issues found."
    else
        printfn $"Found {issues.Length} open issue(s)."
        for issue in issues do
            printfn $"\n  === #{issue.Number}: {issue.Title} ==="
            let full = fetchIssueWithComments issue
            PixogramRequests.Workflow.run full

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
    configSets
    |> List.find (fun cs -> cs.Name = selectedConfig)
    |> applyConfigSet
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
            | "Triage" -> doTriage issues
            | "Run Workflow" -> doWorkflow issues
            | "Show Conversation" -> doShowConversation issues
            | _ -> ()
