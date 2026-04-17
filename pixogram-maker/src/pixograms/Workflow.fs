module PixogramRequests.Workflow

open System
open System.Diagnostics
open System.IO
open AiBase.Agent
open AiBase.AgentSelection
open PixogramRequests.Config
open PixogramRequests.Conversation
open PixogramRequests.GitHub
open PixogramRequests.Triage

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private extractCodeFromMarkdown (response: string) : string =
    let codeBlockPattern = System.Text.RegularExpressions.Regex(@"```(?:csharp|cs)?\s*\n([\s\S]*?)```", System.Text.RegularExpressions.RegexOptions.Compiled)
    let matches = codeBlockPattern.Matches(response)
    if matches.Count > 0 then
        matches.[matches.Count - 1].Groups.[1].Value.Trim()
    else
        // Fallback: if no markdown block, treat entire response as code (backwards compat)
        response.Trim()

let private runProcess cmd (args: string list) (env: (string * string) list) =
    let psi = ProcessStartInfo(cmd)
    for a in args do psi.ArgumentList.Add a
    for key, value in env do psi.Environment.[key] <- value
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.UseShellExecute <- false
    psi.CreateNoWindow <- true
    use p = Process.Start psi
    let stdout = p.StandardOutput.ReadToEnd().Trim()
    let stderr = p.StandardError.ReadToEnd().Trim()
    p.WaitForExit 30_000 |> ignore
    if p.ExitCode = 0 then Some stdout
    else
        let cmdLine = $"{cmd} {String.Join(' ', args)}"
        printfn $"    ⚠ Process failed (exit {p.ExitCode}): {cmdLine}"
        if stderr.Length > 0 then printfn $"      stderr: {stderr}"
        None

let private ghEnv =
    match Environment.GetEnvironmentVariable "GITHUB_REPO_PAT" with
    | null | "" -> []
    | pat -> [ "GH_TOKEN", pat ]


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

let private ts () = DateTime.Now.ToString "HH:mm:ss"

let private configSetMarkdown (models: ConfigSet) =
    let impl = backendDisplayName models.Implementor
    let rows =
        [ "Safety Check", backendDisplayName models.SafetyCheck
          "Triage", backendDisplayName models.Triage
          "Director (Visionary)", backendDisplayName models.DirectorVisionary
          "Director (Maverick)", backendDisplayName models.DirectorMaverick
          "Implementor", impl
          match models.ImplementorFallback with
          | Some fb -> "Implementor (Fallback)", backendDisplayName fb
          | None -> ()
          "Compaction", backendDisplayName models.Compaction ]
    let table =
        "| Role | Model |\n|------|-------|\n" +
        (rows |> List.map (fun (role, model) -> $"| {role} | {model} |") |> String.concat "\n")
    $"\n\n---\n\n🤖 **Config Set:** `{models.Name}` · **Implementor:** {impl}" +
    $"\n\n<details>\n<summary>Model Configuration</summary>\n\n{table}\n\n</details>"

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
            printfn $"  [{ts ()}] Render OK ({fileSize} KB)"
            Ok gifPath
        else
            let output = (stdout + "\n" + stderr).Trim()
            printfn $"  [{ts ()}] Render FAILED (exit {proc.ExitCode})"
            Result.Error output
    with ex ->
        Result.Error ex.Message

// ---------------------------------------------------------------------------
// Artifact storage on the pixogram-maker branch
// ---------------------------------------------------------------------------

let private artifactBranch = "pixogram-maker"

let private runGit (args: string list) =
    runProcess "git" args []

let private requireGit (label: string) (args: string list) =
    match runGit args with
    | Some output -> output
    | None -> failwith $"Git command failed: {label} — git {String.Join(' ', args)}"

let private requireProcess (label: string) cmd (args: string list) (env: (string * string) list) =
    match runProcess cmd args env with
    | Some output -> output
    | None -> failwith $"Process failed: {label} — {cmd} {String.Join(' ', args)}"

let private sanitizeForFilesystem (title: string) =
    let cleaned =
        title.ToLowerInvariant()
        |> Seq.choose (fun c ->
            if Char.IsLetterOrDigit c then Some c
            elif c = ' ' || c = '-' || c = '_' then Some '_'
            else None)
        |> Seq.toArray
        |> String
    let collapsed = System.Text.RegularExpressions.Regex.Replace(cleaned, "_+", "_")
    let trimmed = collapsed.Trim('_')
    if trimmed.Length > 40 then trimmed.Substring(0, 40).TrimEnd('_') else trimmed

let private issueFolderName (issueNumber: int) (title: string) =
    let sanitized = sanitizeForFilesystem title
    $"issue-{issueNumber}_{sanitized}"

let private setupWorktree () =
    let worktreePath = Path.Combine(Path.GetTempPath(), "pixogram-maker-wt")
    // Clean up any leftover worktree
    if Directory.Exists worktreePath then
        runGit [ "worktree"; "remove"; worktreePath; "--force" ] |> ignore
    requireGit "fetch branch" [ "fetch"; "origin"; artifactBranch ] |> ignore
    requireGit "add worktree" [ "worktree"; "add"; worktreePath; artifactBranch ] |> ignore
    // Sync local branch with remote (may be behind after local test runs)
    requireProcess "git reset" "git" [ "-C"; worktreePath; "reset"; "--hard"; $"origin/{artifactBranch}" ] [] |> ignore
    // Configure git identity (not set by default in GitHub Actions)
    requireProcess "git config user.email" "git" [ "-C"; worktreePath; "config"; "user.email"; "github-actions[bot]@users.noreply.github.com" ] [] |> ignore
    requireProcess "git config user.name" "git" [ "-C"; worktreePath; "config"; "user.name"; "github-actions[bot]" ] [] |> ignore
    worktreePath

let private cleanupWorktree (worktreePath: string) =
    if Directory.Exists worktreePath then
        runGit [ "worktree"; "remove"; worktreePath; "--force" ] |> ignore

type ArtifactLinks = {
    GifRaw: string
    GifPage: string
    CsPage: string
}

let private commitArtifacts (issueNumber: int) (issueTitle: string) (iteration: int) (csPath: string) (gifPath: string) : ArtifactLinks =
    let folder = issueFolderName issueNumber issueTitle
    let num = iteration.ToString("D3")
    let targetCs = $"{num}.cs"
    let targetGif = $"{num}.gif"

    printfn $"    Committing iteration {iteration} to {artifactBranch}/{folder}/..."

    let worktreePath = setupWorktree ()
    try
        // Create issue folder
        let issueDir = Path.Combine(worktreePath, folder)
        Directory.CreateDirectory issueDir |> ignore

        // Copy files
        File.Copy(csPath, Path.Combine(issueDir, targetCs), overwrite = true)
        File.Copy(gifPath, Path.Combine(issueDir, targetGif), overwrite = true)

        // Commit and push
        requireProcess "git add" "git" [ "-C"; worktreePath; "add"; $"{folder}/{targetCs}"; $"{folder}/{targetGif}" ] [] |> ignore
        requireProcess "git commit" "git" [ "-C"; worktreePath; "commit"; "-m"; $"#{issueNumber} iteration {iteration}" ] [] |> ignore
        requireProcess "git push" "git" [ "-C"; worktreePath; "push"; "origin"; artifactBranch ] [] |> ignore

        let baseUrl = $"https://github.com/{owner}/{repoName}/blob/{artifactBranch}/{folder}"
        let links = {
            GifRaw = $"{baseUrl}/{targetGif}?raw=true"
            GifPage = $"{baseUrl}/{targetGif}"
            CsPage = $"{baseUrl}/{targetCs}"
        }
        printfn $"    ✓ Committed to {artifactBranch}/{folder}/, GIF: {links.GifPage}"
        links
    finally
        cleanupWorktree worktreePath

// ---------------------------------------------------------------------------
// Issue-body gallery (all pixograms from this issue, rendered in the body)
// ---------------------------------------------------------------------------

let private galleryStartMarker = "<!-- pixogram-gallery:start -->"
let private galleryEndMarker = "<!-- pixogram-gallery:end -->"

let private buildGalleryBlock (issueNumber: int) (issueTitle: string) : string option =
    let folder = issueFolderName issueNumber issueTitle
    let files = listBranchFolder artifactBranch folder
    let gifs =
        files
        |> List.filter (fun f -> f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        |> List.sort
    if gifs.IsEmpty then None
    else
        let folderUrl = $"https://github.com/{owner}/{repoName}/tree/{artifactBranch}/{folder}"
        let cell (gif: string) =
            let trimmed = gif.Replace(".gif", "").TrimStart('0')
            let iter = if trimmed = "" then "0" else trimmed
            let url = $"https://github.com/{owner}/{repoName}/blob/{artifactBranch}/{folder}/{gif}?raw=true"
            $"![Iter {iter}]({url})<br>**Iter {iter}**"
        let rows =
            gifs
            |> List.chunkBySize 4
            |> List.map (fun chunk ->
                let cells = chunk |> List.map cell
                let padded = cells @ List.replicate (4 - cells.Length) ""
                "| " + (padded |> String.concat " | ") + " |")
            |> String.concat "\n"
        let header = "|  |  |  |  |\n|:-:|:-:|:-:|:-:|"
        let block =
            $"{galleryStartMarker}\n" +
            $"> 🤖 Diese Galerie wird automatisch vom **pixogram-maker**-Bot aktualisiert.\n" +
            $"> Sie enthält alle bisher erzeugten Pixogramme aus diesem Issue " +
            $"(Branch [`{artifactBranch}/{folder}`]({folderUrl})).\n\n" +
            $"{header}\n{rows}\n" +
            $"{galleryEndMarker}"
        Some block

let private replaceOrAppendGallery (originalBody: string) (galleryBlock: string) : string =
    let body = if isNull originalBody then "" else originalBody
    let startIdx = body.IndexOf galleryStartMarker
    let endIdx = body.IndexOf galleryEndMarker
    if startIdx >= 0 && endIdx > startIdx then
        let before = body.Substring(0, startIdx).TrimEnd()
        let after = body.Substring(endIdx + galleryEndMarker.Length).TrimStart()
        let tail = if after.Length > 0 then $"\n\n{after}" else ""
        $"{before}\n\n{galleryBlock}{tail}"
    else
        $"{body.TrimEnd()}\n\n{galleryBlock}"

let updateIssueGallery (issueNumber: int) (issueTitle: string) =
    try
        match buildGalleryBlock issueNumber issueTitle with
        | None ->
            printfn $"    Gallery: no GIFs yet on {artifactBranch}, skipping."
        | Some block ->
            let oldBody = fetchIssueBody issueNumber
            let newBody = replaceOrAppendGallery oldBody block
            if newBody = oldBody then
                printfn $"    Gallery: body unchanged."
            else
                updateIssueBody issueNumber newBody
                printfn $"    Gallery: issue body updated with latest pixograms."
    with ex ->
        printfn $"    ⚠ Gallery update failed: {ex.Message}"

// ---------------------------------------------------------------------------
// Compaction (stored in issue folder on pixogram-maker branch)
// ---------------------------------------------------------------------------

let private compactionFileName = "compaction.md"

let private downloadCompaction (issueNumber: int) (issueTitle: string) : string option =
    let folder = issueFolderName issueNumber issueTitle
    let path = $"{folder}/{compactionFileName}"
    match runProcess "gh" [ "api"; $"repos/{owner}/{repoName}/contents/{path}"; "--jq"; ".content"; "-H"; "Accept: application/vnd.github.v3+json"; "--method"; "GET"; "-f"; $"ref={artifactBranch}" ] ghEnv with
    | Some base64Content ->
        try
            let content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Content.Replace("\n", "")))
            if content.Length > 0 then
                printfn $"  ✓ Loaded compaction summary ({content.Length} chars)"
                Some content
            else None
        with _ -> None
    | None -> None

let private uploadCompaction (issueNumber: int) (issueTitle: string) (summary: string) =
    let folder = issueFolderName issueNumber issueTitle
    let worktreePath = setupWorktree ()
    try
        let issueDir = Path.Combine(worktreePath, folder)
        Directory.CreateDirectory issueDir |> ignore
        File.WriteAllText(Path.Combine(issueDir, compactionFileName), summary)
        requireProcess "git add" "git" [ "-C"; worktreePath; "add"; $"{folder}/{compactionFileName}" ] [] |> ignore
        requireProcess "git commit" "git" [ "-C"; worktreePath; "commit"; "-m"; $"#{issueNumber} update compaction" ] [] |> ignore
        requireProcess "git push" "git" [ "-C"; worktreePath; "push"; "origin"; artifactBranch ] [] |> ignore
        printfn $"    ✓ Compaction summary committed to {artifactBranch}/{folder}/"
    finally
        cleanupWorktree worktreePath

let private runCompaction (config: PipelineConfig) (protocol: ProtocolLog) (conversationMessages: ChatMessage list) (issueNumber: int) (issueTitle: string) : string =
    printfn $"  [{ts ()}] ▶ Running compaction..."
    let conversationText = renderConversationAsText conversationMessages
    let systemPrompt = renderSystemPrompt "compaction.md" []
    let messages = [ ChatMessage.system systemPrompt; ChatMessage.user conversationText ]
    match askChat config.Models.Compaction config.AiTimeoutMs messages with
    | Result.Error err ->
        printfn $"  ✗ Compaction failed: {err}"
        log protocol "Compaction" $"FAILED: {err}"
        ""
    | Ok summary ->
        printfn $"  ✓ Compaction done ({summary.Length} chars, ~{estimateTokens summary} tokens)"
        log protocol "Compaction" summary
        uploadCompaction issueNumber issueTitle summary
        summary

let private needsCompaction (config: PipelineConfig) (conversationMessages: ChatMessage list) =
    let text = renderConversationAsText conversationMessages
    let tokens = estimateTokens text
    let threshold = int (float config.Models.ContextLengthTokens * config.Models.CompactionThreshold)
    let needs = tokens >= threshold
    if needs then
        printfn $"  ⚠ Conversation exceeds compaction threshold ({tokens} tokens >= {threshold})"
    needs

// ---------------------------------------------------------------------------
// Step execution
// ---------------------------------------------------------------------------

let private executeDirector (config: PipelineConfig) (protocol: ProtocolLog) (backend: SelectedBackend) (promptFile: string) (label: string) (conversationMessages: ChatMessage list) (issueNumber: int) =
    let expectedTag = $"**[{label}]**"
    let mutable attempt = 1
    let mutable posted = false
    while not posted && attempt <= config.MaxDirectorRetries do
        printfn $"  [{ts ()}] ▶ Running {label} ({backendDisplayName backend}), attempt {attempt}/{config.MaxDirectorRetries}..."
        printfn $"    Prompt: {promptFile}"
        match callAgent backend config.AiTimeoutMs promptFile conversationMessages with
        | Result.Error err ->
            printfn $"  ✗ {label} failed: {err}"
            log protocol label $"FAILED (attempt {attempt}): {err}"
        | Ok response when not (response.Contains(expectedTag)) ->
            printfn $"  ✗ {label} response missing expected tag '{expectedTag}', discarding."
            log protocol label $"DISCARDED (attempt {attempt}, missing tag): {response}"
        | Ok response ->
            log protocol label response
            printfn $"    Posting comment..."
            postComment issueNumber response
            printfn $"  ✓ {label} posted."
            posted <- true
        attempt <- attempt + 1
    if not posted then
        printfn $"  ✗ {label} failed after {config.MaxDirectorRetries} attempts."

let private generateSummary (config: PipelineConfig) (conversationMessages: ChatMessage list) =
    printfn $"    [{ts ()}] Generating summary..."
    let stripped = stripDetailsFromMessages conversationMessages
    let conversationText = renderConversationAsText stripped
    let prompt = renderPrompt "summary.md" [ "conversation", conversationText ]
    let messages = [ ChatMessage.system noToolsPrompt; ChatMessage.user prompt ]
    match askChat config.Models.Triage config.AiTimeoutMs messages with
    | Ok summary -> summary.Trim()
    | Result.Error err ->
        printfn $"    ✗ Summary failed: {err}"
        ""

let private executeImplementor (config: PipelineConfig) (protocol: ProtocolLog) (conversationMessages: ChatMessage list) (fullConversationMessages: ChatMessage list) (comments: IssueComment list) (issueNumber: int) (issueTitle: string) : bool =
    printfn $"  [{ts ()}] ▶ Running Implementor ({backendDisplayName config.Models.Implementor})..."
    printfn "    Prompt: implementor.md"

    let iterationNumber = countImplementorComments comments + 1
    printfn $"    Iteration: #{iterationNumber}"

    let implTokens = conversationMessages |> renderConversationAsText |> estimateTokens
    printfn $"    Implementor conversation: ~{implTokens} tokens"

    let systemPrompt = renderSystemPrompt "implementor.md" [ "api_reference", apiReference.Value ]
    let baseMessages = ChatMessage.system systemPrompt :: conversationMessages

    // Try primary model, fall back if response is empty or missing code marker
    let getInitialCode (backend: SelectedBackend) =
        printfn $"    [{ts ()}] [impl] Sending to {backendDisplayName backend}..."
        match askChat backend config.AiTimeoutMs baseMessages with
        | Ok response ->
            let code = extractCodeFromMarkdown response
            if code.Contains("// ---") then
                printfn $"    [impl] Code extracted ({code.Length} chars from {response.Length} chars response)"
                Some code
            else
                printfn $"  ⚠ Implementor response missing '// ---' marker ({code.Length} chars)"
                log protocol "Implementor" $"INVALID (missing marker, {code.Length} chars): {code.[..min 200 (code.Length - 1)]}"
                None
        | Result.Error err ->
            printfn $"  ✗ Implementor AI failed: {err}"
            log protocol "Implementor" $"FAILED: {err}"
            None

    let initialCode =
        match getInitialCode config.Models.Implementor with
        | Some code -> Some code
        | None ->
            match config.Models.ImplementorFallback with
            | Some fallback ->
                printfn $"  ↩ Trying fallback model ({backendDisplayName fallback})..."
                log protocol "Implementor" $"FALLBACK: switching to {backendDisplayName fallback}"
                getInitialCode fallback
            | None ->
                printfn $"  ✗ No fallback model configured."
                None

    match initialCode with
    | None ->
        printfn $"  ✗ Implementor could not produce valid code."
        log protocol "Implementor" "GAVE UP: no valid code from primary or fallback"
        false
    | Some initialCode ->

    // Render-retry loop: accumulate message history for each retry
    let mutable retryMessages = baseMessages @ [ ChatMessage.assistant initialCode ]
    let mutable code = initialCode
    let mutable attempt = 1
    let mutable success = false

    while not success && attempt <= config.MaxImplementorRetries do
        printfn $"    [{ts ()}] [impl] Render attempt {attempt}/{config.MaxImplementorRetries}..."
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
            let links = commitArtifacts issueNumber issueTitle iterationNumber csPath gifPath
            let gifMarkdown = $"\n\n![preview]({links.GifRaw})"
            let summary = generateSummary config fullConversationMessages
            let summaryLine = if summary <> "" then $"\n\n{summary}" else ""
            let vscodeUrl = $"https://vscode.dev/github/{owner}/{repoName}/blob/{artifactBranch}/{issueFolderName issueNumber issueTitle}"
            let openLinks =
                $"\n\n[GIF]({links.GifPage}) · " +
                $"[C# code]({links.CsPage}) · " +
                $"[Open in VS Code]({vscodeUrl})"
            let comment =
                $"{roleTag Role.Implementor} — Iteration {iterationNumber}" +
                summaryLine +
                gifMarkdown +
                openLinks +
                configSetMarkdown config.Models
            postComment issueNumber comment
            printfn $"  [{ts ()}] ✓ Implementor posted — iteration {iterationNumber} (attempt {attempt})."
            updateIssueGallery issueNumber issueTitle
            success <- true

        | Result.Error err ->
            printfn $"    Render failed (attempt {attempt}): {err}"
            log protocol "Render" $"ATTEMPT {attempt} FAILED: {err}"

            if attempt < config.MaxImplementorRetries then
                let feedback =
                    "The code failed to compile/render. Here is the error:\n\n" +
                    $"```\n{err}\n```\n\n" +
                    "Analyze what went wrong, then output the corrected complete C# code in a ```csharp block."
                retryMessages <- retryMessages @ [ ChatMessage.user feedback ]
                printfn $"    [impl] Sending error feedback (attempt {attempt})..."
                match askChat config.Models.Implementor config.AiTimeoutMs retryMessages with
                | Result.Error aiErr ->
                    printfn $"  ✗ Implementor retry AI failed: {aiErr}"
                    log protocol "Implementor" $"RETRY AI FAILED: {aiErr}"
                    attempt <- config.MaxImplementorRetries // bail out
                | Ok response ->
                    let fixedCode = extractCodeFromMarkdown response
                    printfn $"    [impl] Retry code extracted ({fixedCode.Length} chars from {response.Length} chars response)"
                    retryMessages <- retryMessages @ [ ChatMessage.assistant fixedCode ]
                    code <- fixedCode

        attempt <- attempt + 1

    if not success then
        printfn $"  [{ts ()}] ✗ Implementor failed after {attempt - 1} attempts, not posting to GitHub."
        log protocol "Implementor" $"GAVE UP after {attempt - 1} attempts"

    success

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
        // Pipeline mid-cycle (Director posted, Implementor pending)
        | Some CommentRole.Visionary | Some CommentRole.Maverick ->
            true
        // Last was Implementor → check if still under iteration limit
        | Some CommentRole.Implementor ->
            countImplementorComments issue.Comments < config.DefaultIterations
        // No pipeline comments at all → first run needed
        | None ->
            true
        // Last was user/maintainer → needs attention if no implementor has run yet
        | _ ->
            countImplementorComments issue.Comments = 0

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

        let mutable maxIterations = extractIterationCount config issue.Body
        printfn $"  Max iterations: {maxIterations}"

        // Load existing compaction (if any)
        let mutable compaction = downloadCompaction issue.Number issue.Title

        // Safety valve: hard cap on loop iterations to prevent runaway loops.
        // Recomputed from *current* maxIterations each pass so that auto-iteration
        // bumps (which raise maxIterations mid-run) also raise the loop-step ceiling.
        // Normal cycle = Director + Implementor = 2 iterations; *3+5 is generous.
        let mutable loopStep = 0
        let mutable running = true

        // Cache: skip redundant safety checks and triage when conversation is unchanged
        let mutable lastSafetyCheckedCommentId: int64 option = None
        let mutable lastTriageCommentCount = -1
        let mutable lastTriageResult: NextAction option = None
        while running do
            loopStep <- loopStep + 1
            let maxLoopSteps = maxIterations * 3 + 5
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

            // Safety-check user/maintainer comments before processing (cached by comment ID)
            match lastUserOrMaintainerComment config current with
            | Some userComment when lastSafetyCheckedCommentId = Some userComment.Id ->
                printfn $"  ↩ Comment safety check skipped (unchanged, comment {userComment.Id})"
            | Some userComment ->
                printfn $"  Last comment is from user/maintainer — running safety check..."
                let safetyContext = buildCommentSafetyContext config current
                match runCommentSafetyCheck config safetyContext userComment.Author userComment.Body with
                | SafetyResult.Passed ->
                    printfn $"  ✓ Comment safety check passed."
                    log protocol "CommentSafety" "PASSED"
                    lastSafetyCheckedCommentId <- Some userComment.Id

                    // Check whether the comment asks for N more automatic iterations.
                    // If so, raise the ceiling so the loop keeps going past the original max.
                    let bump = extractIterationBump config userComment.Author userComment.Body
                    if bump > 0 then
                        let implCount = countImplementorComments current.Comments
                        let desired = implCount + bump
                        let newMax = min desired config.MaxIterationsCap
                        if newMax > maxIterations then
                            let cappedNote = if newMax < desired then $" (capped by MAX_ITERATIONS_CAP={config.MaxIterationsCap})" else ""
                            printfn $"  ⤴ User requested +{bump} auto-iterations → maxIterations: {maxIterations} → {newMax}{cappedNote}"
                            log protocol "Workflow" $"User bumped maxIterations: {maxIterations} → {newMax} (+{bump}, impl={implCount}){cappedNote}"
                            maxIterations <- newMax
                        else
                            printfn $"  ⤴ User requested +{bump} but current max {maxIterations} already covers it — no change."
                | SafetyResult.Failed reason ->
                    printfn $"  ✗ Comment safety check failed: {reason}"
                    log protocol "CommentSafety" $"FAILED: {reason}"
                    removeLabel current.Number labelApproved
                    printfn $"  ✗ Removed '{labelApproved}' label. Workflow aborted."
                    running <- false
                | SafetyResult.Error reason ->
                    printfn $"  ✗ Comment safety check error: {reason}"
                    log protocol "CommentSafety" $"ERROR: {reason}"
                    running <- false
            | None -> ()

            if not running then () else

            // Build full conversation (without compaction) to check size
            let rawFullConversation = buildConversation config ConversationView.Full None current
            let tokens = rawFullConversation |> renderConversationAsText |> estimateTokens
            let threshold = int (float config.Models.ContextLengthTokens * config.Models.CompactionThreshold)
            printfn $"  Conversation: ~{tokens} tokens (threshold: {threshold})"

            // Run compaction if needed
            if needsCompaction config rawFullConversation then
                let summary = runCompaction config protocol rawFullConversation current.Number current.Title
                if summary.Length > 0 then
                    compaction <- Some summary
                    log protocol "Compaction" $"Compacted to {summary.Length} chars"

            // Build conversations: Full for Directors/Triage, Implementor view for code generation
            let fullConversation = buildConversation config ConversationView.Full compaction current
            let implConversation = buildConversation config ConversationView.Implementor compaction current
            printfn ""

            if implCount >= maxIterations && not (hasUserFeedbackAfterLastImplementor config current) then
                printfn $"  Max iterations reached ({implCount}/{maxIterations}) — stopping."
                log protocol "Workflow" $"Max iterations reached ({implCount}/{maxIterations})"
                running <- false
            else

            // Deterministic routing: if the last comment is a Director,
            // the next step is always Implementor — no need to ask Triage.
            // Triage is only needed for genuine decision points:
            //   - After Implementor (which Director next? DONE?)
            //   - After user feedback on Implementor (what kind of feedback?)
            //   - First step (no pipeline comments yet)
            let lastRole = lastCommentRole config current
            let action =
                match lastRole with
                | Some CommentRole.Visionary | Some CommentRole.Maverick ->
                    // Director posted → always Implementor next (deterministic)
                    printfn $"  [deterministic] Last comment is Director → IMPLEMENTOR"
                    log protocol "Routing" "Deterministic: Director → IMPLEMENTOR"
                    RunImplementor
                | _ ->
                    // Genuine decision point → ask Triage AI (cached by comment count)
                    if current.Comments.Length = lastTriageCommentCount && lastTriageResult.IsSome then
                        printfn $"  ↩ Triage skipped (unchanged, {current.Comments.Length} comments)"
                        log protocol "Triage" $"CACHED: {lastTriageResult.Value}"
                        lastTriageResult.Value
                    else
                        // Strip code from Implementor messages — Triage only needs conversation structure
                        let triageConversation = stripDetailsFromMessages fullConversation
                        let triageTokens = triageConversation |> renderConversationAsText |> estimateTokens
                        printfn $"  Triage conversation: ~{triageTokens} tokens (stripped code from Implementor messages)"
                        let triageAction = determineNextAction config maxIterations current.Author triageConversation
                        log protocol "Triage" $"{triageAction}"
                        lastTriageCommentCount <- current.Comments.Length
                        lastTriageResult <- Some triageAction
                        triageAction
            printfn ""

            match action with
            | RunVisionary ->
                executeDirector config protocol config.Models.DirectorVisionary "director-visionary.md" "Director/Visionary" fullConversation current.Number
            | RunMaverick ->
                executeDirector config protocol config.Models.DirectorMaverick "director-maverick.md" "Director/Maverick" fullConversation current.Number
            | RunImplementor ->
                let succeeded = executeImplementor config protocol implConversation fullConversation current.Comments current.Number current.Title
                if not succeeded then
                    printfn "  Implementor exhausted retries — stopping workflow (re-run on next trigger)."
                    log protocol "Workflow" "Implementor exhausted retries — stopping workflow"
                    running <- false
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
