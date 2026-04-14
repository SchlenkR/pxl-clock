module PixogramRequests.Workflow

open System
open System.Diagnostics
open System.IO
open AiBase.AgentSelection
open PixogramRequests.Config
open PixogramRequests.Conversation
open PixogramRequests.GitHub
open PixogramRequests.Triage

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private ghRepo = $"{owner}/{repoName}"

let private runProcess cmd (args: string list) (env: (string * string) list) =
    let psi = ProcessStartInfo(cmd)
    for a in args do psi.ArgumentList.Add a
    for key, value in env do psi.Environment.[key] <- value
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.UseShellExecute <- false
    psi.CreateNoWindow <- true
    use p = Process.Start psi
    let output = p.StandardOutput.ReadToEnd().Trim()
    p.WaitForExit 30_000 |> ignore
    if p.ExitCode = 0 then Some output else None

let private ghEnv =
    match Environment.GetEnvironmentVariable "GITHUB_REPO_PAT" with
    | null | "" -> []
    | pat -> [ "GH_TOKEN", pat ]

let private runGh (args: string list) =
    runProcess "gh" (args @ [ "--repo"; ghRepo ]) ghEnv

// ---------------------------------------------------------------------------
// Protocol logging
// ---------------------------------------------------------------------------

let private outputDir = Path.Combine(projectDir, "output")

type private ProtocolLog =
    {
        Writer: StreamWriter
        Dir: string
    }

let private startProtocol (issueNumber: int) =
    let issueDir = Path.Combine(outputDir, $"issue-{issueNumber}")
    Directory.CreateDirectory issueDir |> ignore
    let timestamp = DateTime.Now.ToString "yyyy-MM-dd_HH-mm-ss"
    let logPath = Path.Combine(issueDir, $"{timestamp}.log")
    let writer = new StreamWriter(logPath, append = false)
    writer.AutoFlush <- true
    writer.WriteLine $"# Workflow Protocol — Issue #{issueNumber}"
    let now = DateTime.Now.ToString "O"
    writer.WriteLine $"# Started: {now}"
    writer.WriteLine()
    {
        Writer = writer
        Dir = issueDir
    }

let private log (protocol: ProtocolLog) (role: string) (text: string) =
    let ts = DateTime.Now.ToString "HH:mm:ss"
    protocol.Writer.WriteLine $"[{ts}] [{role}]"
    protocol.Writer.WriteLine text
    protocol.Writer.WriteLine()

// ---------------------------------------------------------------------------
// Code extraction & rendering
// ---------------------------------------------------------------------------

let private renderPixogram (config: PipelineConfig) (csPath: string) (gifPath: string) : Result<string, string> =
    printfn $"  Rendering: {Path.GetFileName csPath} → {Path.GetFileName gifPath}"
    let psi = ProcessStartInfo "Pxl.Render"
    for a in [ csPath; "--output"; gifPath; "--duration"; string config.GifDurationSeconds; "--scale"; string config.GifScale; "--mode"; "clock" ] do
        psi.ArgumentList.Add a
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.UseShellExecute <- false
    psi.CreateNoWindow <- true
    try
        use proc = Process.Start psi
        let stdout = proc.StandardOutput.ReadToEnd()
        let stderr = proc.StandardError.ReadToEnd()
        proc.WaitForExit(120_000) |> ignore
        if proc.ExitCode = 0 then
            let fileSize = FileInfo(gifPath).Length / 1024L
            printfn $"  Render OK ({fileSize} KB)"
            Ok gifPath
        else
            let output = (stdout + "\n" + stderr).Trim()
            printfn $"  Render FAILED (exit {proc.ExitCode})"
            Error output
    with ex ->
        Error ex.Message

// ---------------------------------------------------------------------------
// Branch-based artifact storage (one branch per issue)
// ---------------------------------------------------------------------------

let private issueBranch (issueNumber: int) = $"pixogram/issue-{issueNumber}"

let private runGit (args: string list) =
    runProcess "git" args []

let private commitToIssueBranch (issueNumber: int) (iteration: int) (csPath: string) (gifPath: string) : string =
    let branch = issueBranch issueNumber
    let repoUrl = $"https://github.com/{owner}/{repoName}"
    let targetCs = "pixogram.cs"
    let targetGif = "preview.gif"

    printfn $"    Committing iteration #{iteration} to branch '{branch}'..."

    // Use a temporary worktree to avoid switching the main checkout
    let worktreePath = Path.Combine(Path.GetTempPath(), $"pixogram-wt-{issueNumber}")
    try
        // Clean up any leftover worktree
        if Directory.Exists worktreePath then
            runGit [ "worktree"; "remove"; worktreePath; "--force" ] |> ignore

        // Create orphan branch or fetch existing
        let branchExists =
            runGit [ "ls-remote"; "--heads"; "origin"; branch ]
            |> Option.map (fun s -> s.Length > 0)
            |> Option.defaultValue false

        if branchExists then
            runGit [ "fetch"; "origin"; branch ] |> ignore
            runGit [ "worktree"; "add"; worktreePath; branch ] |> ignore
        else
            runGit [ "worktree"; "add"; "--orphan"; worktreePath; "-b"; branch ] |> ignore
            // Remove all files from orphan worktree
            for f in Directory.GetFiles(worktreePath) do
                if not (Path.GetFileName(f).StartsWith(".")) then
                    File.Delete f

        // Copy files to worktree
        File.Copy(csPath, Path.Combine(worktreePath, targetCs), overwrite = true)
        File.Copy(gifPath, Path.Combine(worktreePath, targetGif), overwrite = true)

        // Commit and push from worktree
        runProcess "git" [ "-C"; worktreePath; "add"; targetCs; targetGif ] [] |> ignore
        runProcess "git" [ "-C"; worktreePath; "commit"; "-m"; $"Iteration #{iteration}" ] [] |> ignore
        runProcess "git" [ "-C"; worktreePath; "push"; "-u"; "origin"; branch ] [] |> ignore

        let gifUrl = $"{repoUrl}/blob/{branch}/{targetGif}?raw=true"
        printfn $"    ✓ Committed to {branch}, GIF: {gifUrl}"
        gifUrl
    finally
        // Always clean up worktree
        if Directory.Exists worktreePath then
            runGit [ "worktree"; "remove"; worktreePath; "--force" ] |> ignore

// ---------------------------------------------------------------------------
// Compaction (stored on issue branch)
// ---------------------------------------------------------------------------

let private compactionFileName = "compaction.md"

let private downloadCompaction (issueNumber: int) : string option =
    let branch = issueBranch issueNumber
    // Try to read compaction.md from the issue branch via GitHub API
    match runGh [ "api"; $"repos/{owner}/{repoName}/contents/{compactionFileName}"; "--jq"; ".content"; "-H"; "Accept: application/vnd.github.v3+json"; "--method"; "GET"; "-f"; $"ref={branch}" ] with
    | Some base64Content ->
        try
            let content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Content.Replace("\n", "")))
            if content.Length > 0 then
                printfn $"  ✓ Loaded compaction summary ({content.Length} chars)"
                Some content
            else None
        with _ -> None
    | None -> None

let private uploadCompaction (issueNumber: int) (summary: string) =
    let branch = issueBranch issueNumber
    let branchExists =
        runGit [ "ls-remote"; "--heads"; "origin"; branch ]
        |> Option.map (fun s -> s.Length > 0)
        |> Option.defaultValue false

    if branchExists then
        let worktreePath = Path.Combine(Path.GetTempPath(), $"pixogram-wt-compact-{issueNumber}")
        try
            if Directory.Exists worktreePath then
                runGit [ "worktree"; "remove"; worktreePath; "--force" ] |> ignore
            runGit [ "fetch"; "origin"; branch ] |> ignore
            runGit [ "worktree"; "add"; worktreePath; branch ] |> ignore
            File.WriteAllText(Path.Combine(worktreePath, compactionFileName), summary)
            runProcess "git" [ "-C"; worktreePath; "add"; compactionFileName ] [] |> ignore
            runProcess "git" [ "-C"; worktreePath; "commit"; "-m"; "Update compaction summary" ] [] |> ignore
            runProcess "git" [ "-C"; worktreePath; "push"; "origin"; branch ] [] |> ignore
            printfn $"    ✓ Compaction summary committed to {branch}"
        finally
            if Directory.Exists worktreePath then
                runGit [ "worktree"; "remove"; worktreePath; "--force" ] |> ignore

let private runCompaction (config: PipelineConfig) (protocol: ProtocolLog) (fullConversation: string) (issueNumber: int) : string =
    printfn $"  ▶ Running compaction..."
    let prompt = renderPrompt "compaction.md" [ "conversation", fullConversation ]
    match askAI config.Models.Compaction config.AiTimeoutMs prompt with
    | Error err ->
        printfn $"  ✗ Compaction failed: {err}"
        log protocol "Compaction" $"FAILED: {err}"
        ""
    | Ok summary ->
        printfn $"  ✓ Compaction done ({summary.Length} chars, ~{estimateTokens summary} tokens)"
        log protocol "Compaction" summary
        uploadCompaction issueNumber summary
        summary

let private needsCompaction (config: PipelineConfig) (conversationText: string) =
    let tokens = estimateTokens conversationText
    let threshold = int (float config.Models.ContextLengthTokens * config.Models.CompactionThreshold)
    let needs = tokens >= threshold
    if needs then
        printfn $"  ⚠ Conversation exceeds compaction threshold ({tokens} tokens >= {threshold})"
    needs

// ---------------------------------------------------------------------------
// Step execution
// ---------------------------------------------------------------------------

let private executeDirector (config: PipelineConfig) (protocol: ProtocolLog) (backend: SelectedBackend) (promptFile: string) (label: string) (conversation: string) (issueNumber: int) =
    printfn $"  ▶ Running {label} ({backendDisplayName backend})..."
    printfn $"    Prompt: {promptFile}"
    match callAgent backend config.AiTimeoutMs promptFile conversation with
    | Error err ->
        printfn $"  ✗ {label} failed: {err}"
        log protocol label $"FAILED: {err}"
    | Ok response ->
        log protocol label response
        printfn $"    Posting comment..."
        postComment issueNumber response
        printfn $"  ✓ {label} posted."

let private executeCraftsman (config: PipelineConfig) (protocol: ProtocolLog) (conversation: string) : string option =
    printfn $"  ▶ Running Craftsman ({backendDisplayName config.Models.Craftsman})..."
    printfn $"    Prompt: director-craftsman.md"
    match callAgent config.Models.Craftsman config.AiTimeoutMs "director-craftsman.md" conversation with
    | Error err ->
        printfn $"  ✗ Craftsman failed: {err}"
        log protocol "Craftsman" $"FAILED: {err}"
        None
    | Ok response ->
        log protocol "Craftsman" response
        printfn $"  ✓ Craftsman done (not posting, will embed in Implementor comment)."
        Some response

let private generateSummary (config: PipelineConfig) (conversation: string) =
    printfn "    Generating summary..."
    match askAI config.Models.Triage config.AiTimeoutMs (renderPrompt "summary.md" [ "conversation", conversation ]) with
    | Ok summary -> summary.Trim()
    | Error err ->
        printfn $"    ✗ Summary failed: {err}"
        ""

let private executeImplementor (config: PipelineConfig) (protocol: ProtocolLog) (conversation: string) (fullConversation: string) (craftsmanText: string option) (comments: IssueComment list) (issueNumber: int) =
    printfn $"  ▶ Running Implementor ({backendDisplayName config.Models.Implementor})..."
    printfn "    Prompt: implementor.md"

    let iterationNumber = countImplementorComments comments + 1
    printfn $"    Iteration: #{iterationNumber}"

    let prompt = renderPrompt "implementor.md" [ "conversation", conversation ]

    use agent = createAgent config.Models.Implementor
    printfn $"    [impl] Agent created, sending initial prompt..."

    // Step 1: Get initial code
    match sendToAgent agent config.AiTimeoutMs prompt with
    | Error err ->
        printfn $"  ✗ Implementor AI failed: {err}"
        log protocol "Implementor" $"FAILED: {err}"
    | Ok initialCode ->

    let mutable code = initialCode
    let mutable attempt = 1
    let mutable success = false

    while not success && attempt <= config.MaxImplementorRetries do
        printfn $"    [impl] Render attempt {attempt}/{config.MaxImplementorRetries}..."
        log protocol "Implementor" $"ATTEMPT {attempt}:\n{code}"

        let timestamp = DateTime.Now.ToString "yyyy-MM-dd_HH-mm-ss"
        let csPath = Path.Combine(protocol.Dir, $"{timestamp}.cs")
        let gifPath = Path.Combine(protocol.Dir, $"{timestamp}.gif")
        File.WriteAllText(csPath, code)
        printfn $"    Code saved: {csPath}"

        match renderPixogram config csPath gifPath with
        | Ok _ ->
            printfn $"    Render OK"
            log protocol "Render" $"OK: {gifPath}"
            let gifUrl = commitToIssueBranch issueNumber iterationNumber csPath gifPath
            let gifMarkdown = $"\n\n![preview]({gifUrl})"
            let summary = generateSummary config fullConversation
            let summaryLine = if summary <> "" then $"\n\n{summary}" else ""
            let craftsmanBlock =
                match craftsmanText with
                | Some ct ->
                    $"\n\n<details>\n<summary>Craftsman Specification</summary>\n\n" +
                    $"{ct}\n\n</details>"
                | None -> ""
            let branch = issueBranch issueNumber
            let codespaceBadge =
                $"\n\n[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/{owner}/{repoName}/tree/{branch}?quickstart=1)"
            let comment =
                $"{roleTag Role.Implementor} — Iteration #{iterationNumber}" +
                summaryLine +
                gifMarkdown +
                codespaceBadge +
                craftsmanBlock +
                $"\n\n<details>\n<summary>Code anzeigen</summary>\n\n" +
                $"```csharp\n{code}\n```\n\n</details>"
            postComment issueNumber comment
            printfn $"  ✓ Implementor posted — iteration #{iterationNumber} (attempt {attempt})."
            success <- true

        | Error err ->
            printfn $"    Render failed (attempt {attempt}): {err}"
            log protocol "Render" $"ATTEMPT {attempt} FAILED: {err}"

            if attempt < config.MaxImplementorRetries then
                let feedback =
                    "The code failed to compile/render. Here is the error:\n\n" +
                    $"```\n{err}\n```\n\n" +
                    "Please fix the code and output ONLY the corrected raw C# code. No markdown, no explanations."
                printfn $"    [impl] Sending error feedback to same session..."
                match sendToAgent agent config.AiTimeoutMs feedback with
                | Error aiErr ->
                    printfn $"  ✗ Implementor retry AI failed: {aiErr}"
                    log protocol "Implementor" $"RETRY AI FAILED: {aiErr}"
                    attempt <- config.MaxImplementorRetries // bail out
                | Ok fixedCode ->
                    code <- fixedCode

        attempt <- attempt + 1

    if not success then
        printfn $"  ✗ Implementor failed after {attempt - 1} attempts, not posting to GitHub."
        log protocol "Implementor" $"GAVE UP after {attempt - 1} attempts"

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

let private runApprovalGate (config: PipelineConfig) (protocol: ProtocolLog) (issue: Issue) : bool =
    if hasLabel issue.Number labelApproved then
        printfn $"  ✓ Issue #{issue.Number} already approved."
        true
    elif isTrustedAuthor config issue.Author then
        printfn $"  ✓ Issue #{issue.Number} auto-approved (author {issue.Author} is trusted)."
        addLabel issue.Number labelApproved
        log protocol "Approval" $"Auto-approved ({issue.Author} is trusted author)"
        true
    else
        let names = String.Join(", ", config.Maintainers |> List.map (fun m -> $"@{m}"))
        printfn $"  ✗ Issue #{issue.Number} needs approval from a maintainer."
        postComment issue.Number $"{names} Bitte gebt dieses Issue frei (Label `{labelApproved}` setzen)."
        log protocol "Approval" "Waiting for maintainer approval"
        false

let private runSafetyGate (config: PipelineConfig) (protocol: ProtocolLog) (issue: Issue) : bool =
    if hasLabel issue.Number labelIgnore then
        printfn $"  ✗ Issue #{issue.Number} has '{labelIgnore}' label — skipping."
        log protocol "Safety" "Skipped: marked as ignore."
        false
    elif hasLabel issue.Number labelTriagePassed then
        printfn $"  ✓ Issue #{issue.Number} already has '{labelTriagePassed}' label."
        true
    else
        printfn $"  Running safety check for issue #{issue.Number}..."
        match runSafetyCheck config issue with
        | SafetyResult.Passed ->
            printfn $"  ✓ Safety check passed."
            log protocol "Safety" "PASSED"
            addLabel issue.Number labelTriagePassed
            addLabel issue.Number labelPixogramIdea
            true
        | SafetyResult.Failed reason ->
            printfn $"  ✗ Safety check failed: {reason}"
            log protocol "Safety" $"FAILED: {reason}"
            addLabel issue.Number labelIgnore
            false
        | SafetyResult.Error reason ->
            printfn $"  ✗ Safety check error (not marking issue): {reason}"
            log protocol "Safety" $"ERROR: {reason}"
            false

// ---------------------------------------------------------------------------
// Dispatcher: determine which issues need a workflow run (no AI calls)
// ---------------------------------------------------------------------------

/// Pure check: does this issue need a workflow run right now?
/// No AI calls, no side effects beyond label reads — safe for fast scanning.
let needsAttention (config: PipelineConfig) (issue: Issue) : bool =
    // Ignored → skip
    if issue.Labels |> List.exists (fun l -> l = labelIgnore) then
        false
    // Not safety-checked yet → needs safety gate first (is this even a pixogram request?)
    elif not (issue.Labels |> List.exists (fun l -> l = labelTriagePassed)) then
        true
    // Not approved yet → needs approval gate
    elif not (issue.Labels |> List.exists (fun l -> l = labelApproved)) then
        true
    // Has user feedback after last implementor → new work to do
    elif hasUserFeedbackAfterLastImplementor config issue then
        true
    else
        match lastCommentRole config issue with
        // Pipeline mid-cycle (Director or Craftsman posted, next step pending)
        | Some CommentRole.Visionary | Some CommentRole.Maverick | Some CommentRole.Craftsman ->
            true
        // Last was Implementor → check if still under iteration limit
        | Some CommentRole.Implementor ->
            countImplementorComments issue.Comments < config.DefaultIterations
        // No pipeline comments at all → first run needed
        | None ->
            true
        // Last was user/maintainer but not after implementor → already handled above
        | _ ->
            false

/// Scan all eligible issues and return those that need a workflow run.
let dispatch (config: PipelineConfig) : Issue list =
    let issues = listEligibleIssues ()
    printfn $"  Found {issues.Length} eligible issue(s), checking which need attention..."
    issues
    |> List.map (fun issue ->
        let full = fetchIssueWithComments issue
        full, needsAttention config full)
    |> List.filter snd
    |> List.map (fun (issue, _) ->
        printfn $"    #{issue.Number}: {issue.Title} → needs attention"
        issue)

let triageOnly (config: PipelineConfig) (issue: Issue) =
    let protocol = startProtocol issue.Number
    try
        if runSafetyGate config protocol issue then
            runApprovalGate config protocol issue |> ignore
    finally
        protocol.Writer.Dispose()

let run (config: PipelineConfig) (issue: Issue) =
    let protocol = startProtocol issue.Number

    try
        if not (runSafetyGate config protocol issue) then
            printfn "  ─── Workflow aborted (not a pixogram request) ───"
        elif not (runApprovalGate config protocol issue) then
            printfn "  ─── Workflow aborted (not approved) ───"
        else

        let maxIterations = extractIterationCount config issue.Body
        printfn $"  Max iterations: {maxIterations}"

        // Load existing compaction from release (if any)
        let mutable compaction = downloadCompaction issue.Number

        // Safety valve: hard cap on loop iterations to prevent runaway loops.
        // Normal cycle = Director + Craftsman/Implementor = 2 iterations.
        // With user feedback cycles, maxIterations * 3 + 5 is generous.
        let maxLoopSteps = maxIterations * 3 + 5
        let mutable loopStep = 0
        let mutable running = true
        while running do
            loopStep <- loopStep + 1
            if loopStep > maxLoopSteps then
                printfn $"  ⚠ Safety valve: {maxLoopSteps} loop steps exceeded — stopping to prevent runaway."
                log protocol "Workflow" $"SAFETY VALVE: {maxLoopSteps} loop steps exceeded"
                running <- false
            else
            printfn ""
            printfn "  ─── Fetching issue state... ───"
            let current = fetchIssueWithComments issue
            let implCount = countImplementorComments current.Comments
            printfn $"  Issue #{current.Number}: {current.Title}"
            printfn $"  Comments: {current.Comments.Length}, Implementor iterations: {implCount}/{maxIterations}"

            // Build full conversation (without compaction) to check size
            let rawFullConversation = buildConversation config ConversationView.Full None current
            let tokens = estimateTokens rawFullConversation
            let threshold = int (float config.Models.ContextLengthTokens * config.Models.CompactionThreshold)
            printfn $"  Conversation: ~{tokens} tokens (threshold: {threshold})"

            // Run compaction if needed
            if needsCompaction config rawFullConversation then
                let summary = runCompaction config protocol rawFullConversation current.Number
                if summary.Length > 0 then
                    compaction <- Some summary
                    log protocol "Compaction" $"Compacted to {summary.Length} chars"

            // Build conversations using compaction if available
            let fullConversation = buildConversation config ConversationView.Full compaction current
            let implConversation = fullConversation // Implementor gets full context for better cache hits
            printfn ""

            if implCount >= maxIterations && not (hasUserFeedbackAfterLastImplementor config current) then
                printfn $"  Max iterations reached ({implCount}/{maxIterations}) — stopping."
                log protocol "Workflow" $"Max iterations reached ({implCount}/{maxIterations})"
                running <- false
            else

            // Deterministic routing: if the last comment is a Director or Craftsman,
            // the next step is always known — no need to ask Triage (and risk misrouting).
            // Triage is only needed for genuine decision points:
            //   - After Implementor (which Director next? DONE?)
            //   - After user feedback on Implementor (what kind of feedback?)
            //   - First step (no pipeline comments yet)
            let lastRole = lastCommentRole config current
            let action =
                match lastRole with
                | Some CommentRole.Visionary | Some CommentRole.Maverick ->
                    // Director posted → always Craftsman next (deterministic)
                    printfn $"  [deterministic] Last comment is Director → CRAFTSMAN"
                    log protocol "Routing" "Deterministic: Director → CRAFTSMAN"
                    RunCraftsman
                | Some CommentRole.Craftsman ->
                    // Craftsman posted → always Implementor next (deterministic)
                    printfn $"  [deterministic] Last comment is Craftsman → IMPLEMENTOR"
                    log protocol "Routing" "Deterministic: Craftsman → IMPLEMENTOR"
                    RunImplementor
                | _ ->
                    // Genuine decision point → ask Triage AI
                    let triageAction = determineNextAction config maxIterations current.Author fullConversation
                    log protocol "Triage" $"{triageAction}"
                    triageAction
            printfn ""

            match action with
            | RunVisionary ->
                executeDirector config protocol config.Models.DirectorVisionary "director-visionary.md" "Director/Visionary" fullConversation current.Number
            | RunMaverick ->
                executeDirector config protocol config.Models.DirectorMaverick "director-maverick.md" "Director/Maverick" fullConversation current.Number
            | RunCraftsman ->
                match executeCraftsman config protocol fullConversation with
                | None -> ()
                | Some craftsmanResponse ->
                    let craftsmanComment =
                        $"\n<comment id=\"0\" author=\"pipeline\" role=\"craftsman\" time=\"{DateTime.UtcNow:O}\">\n" +
                        $"<![CDATA[{craftsmanResponse}]]>\n" +
                        "</comment>\n"
                    let extendedConversation =
                        implConversation.Replace("</conversation>", craftsmanComment + "</conversation>")
                    executeImplementor config protocol extendedConversation fullConversation (Some craftsmanResponse) current.Comments current.Number
            | RunImplementor ->
                executeImplementor config protocol implConversation fullConversation None current.Comments current.Number
            | Done reason ->
                printfn $"  Done: {reason}"
                running <- false

            printfn ""

        printfn "  ─── Workflow complete ───"
        printfn ""
        log protocol "Workflow" "Finished."
    finally
        let endTime = DateTime.Now.ToString "O"
        protocol.Writer.WriteLine $"# Ended: {endTime}"
        protocol.Writer.Dispose()

/// Dispatch + run: find issues needing attention and run workflow on each.
let dispatchAndRun (config: PipelineConfig) =
    let issues = dispatch config
    if issues.IsEmpty then
        printfn "No issues need attention."
    else
        printfn $"\n{issues.Length} issue(s) need attention, running workflows..."
        for issue in issues do
            printfn $"\n  === #{issue.Number}: {issue.Title} ==="
            run config issue
