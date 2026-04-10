module PixogramRequests.GitHub

open System
open System.Diagnostics

// ---------------------------------------------------------------------------
// Client setup
// ---------------------------------------------------------------------------

let private getToken () =
    match Environment.GetEnvironmentVariable "GITHUB_REPO_PAT" with
    | null | "" ->
        match Environment.GetEnvironmentVariable "GITHUB_TOKEN" with
        | null | "" ->
            let psi = ProcessStartInfo("gh", "auth token")
            psi.RedirectStandardOutput <- true
            psi.UseShellExecute <- false
            psi.CreateNoWindow <- true
            use p = Process.Start psi
            let token = p.StandardOutput.ReadToEnd().Trim()
            p.WaitForExit()
            if p.ExitCode = 0 && token <> "" then token
            else failwith "No GitHub token found. Set GITHUB_REPO_PAT, GITHUB_TOKEN, or run 'gh auth login'."
        | token -> token
    | token -> token

let private client =
    let c = Octokit.GitHubClient(Octokit.ProductHeaderValue "pixogram-requests")
    c.Credentials <- Octokit.Credentials(getToken ())
    c

let private ownerAndRepo =
    match Environment.GetEnvironmentVariable "GITHUB_REPO" with
    | null | "" ->
        let psi = ProcessStartInfo("git", "remote get-url origin")
        psi.RedirectStandardOutput <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        use p = Process.Start psi
        let url = p.StandardOutput.ReadToEnd().Trim()
        p.WaitForExit()
        let parts =
            url.Replace("git@github.com:", "")
                .Replace("https://github.com/", "")
                .TrimEnd('/')
                .Replace(".git", "")
                .Split('/')
        if parts.Length >= 2 then (parts.[parts.Length - 2], parts.[parts.Length - 1])
        else failwith $"Could not parse owner/repo from: {url}"
    | repo ->
        let parts = repo.Split('/')
        if parts.Length = 2 then (parts.[0], parts.[1])
        else failwith $"GITHUB_REPO must be in 'owner/repo' format, got: {repo}"

let owner = fst ownerAndRepo
let repoName = snd ownerAndRepo

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type IssueComment =
    {
        Author: string
        Body: string
        CreatedAt: string
    }

type Issue =
    {
        Number: int
        Title: string
        Labels: string list
        Body: string
        Author: string
        Comments: IssueComment list
    }

// ---------------------------------------------------------------------------
// API calls
// ---------------------------------------------------------------------------

let listIssues () =
    let request = Octokit.RepositoryIssueRequest(State = Octokit.ItemStateFilter.Open)
    let issues = client.Issue.GetAllForRepository(owner, repoName, request).Result
    [ for i in issues ->
        {
            Number = i.Number
            Title = i.Title
            Labels = [ for l in i.Labels -> l.Name ]
            Body = if isNull i.Body then "" else i.Body
            Author = i.User.Login
            Comments = []
        } ]

let fetchComments issueNumber =
    let comments = client.Issue.Comment.GetAllForIssue(owner, repoName, issueNumber).Result
    [ for c in comments ->
        {
            Author = c.User.Login
            Body = c.Body
            CreatedAt = c.CreatedAt.ToString "O"
        } ]

let fetchIssueWithComments (issue: Issue) =
    { issue with Comments = fetchComments issue.Number }

let postComment issueNumber (body: string) =
    client.Issue.Comment.Create(owner, repoName, issueNumber, body).Result |> ignore

let addLabel issueNumber (label: string) =
    client.Issue.Labels.AddToIssue(owner, repoName, issueNumber, [| label |]).Result |> ignore

let fetchLabels issueNumber =
    let issue = client.Issue.Get(owner, repoName, issueNumber).Result
    [ for l in issue.Labels -> l.Name ]

let hasLabel issueNumber (label: string) =
    let labels = fetchLabels issueNumber
    labels |> List.exists (fun l -> String.Equals(l, label, StringComparison.OrdinalIgnoreCase))

let listUntriagedIssues () =
    let triageLabels = set [ "triage-passed"; "triage-failed"; "approved" ]
    listIssues ()
    |> List.filter (fun issue ->
        issue.Labels |> List.exists (fun l -> triageLabels.Contains l) |> not)
