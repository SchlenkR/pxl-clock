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
    // The implementor prompt illustrates nested fences (outer ~~~ wraps inner ```csharp),
    // so models often produce that structure. We prefer the most specific match:
    //   1) Innermost ```csharp / ```cs block (language tag present)
    //   2) Otherwise last bare fence (``` or ~~~)
    // NOTE: In .NET regex, named groups are numbered AFTER unnamed groups, so we name
    // BOTH groups to avoid Groups[index] confusion.
    let opts = System.Text.RegularExpressions.RegexOptions.Compiled
    let csharpPattern =
        System.Text.RegularExpressions.Regex(
            @"```(?:csharp|cs)\s*\n(?<code>[\s\S]*?)```", opts)
    let csharpMatches = csharpPattern.Matches(response)
    if csharpMatches.Count > 0 then
        csharpMatches.[csharpMatches.Count - 1].Groups.["code"].Value.Trim()
    else
        let anyFencePattern =
            System.Text.RegularExpressions.Regex(
                @"(?<fence>```|~~~)\s*\n(?<code>[\s\S]*?)\k<fence>", opts)
        let matches = anyFencePattern.Matches(response)
        if matches.Count > 0 then
            matches.[matches.Count - 1].Groups.["code"].Value.Trim()
        else
            // Fallback: if no markdown block, treat entire response as code (backwards compat)
            response.Trim()

/// Some providers (MiMo V2.5/Pro, GLM 5.1 with high reasoning effort) emit the entire
/// response — code included — into the reasoning stream and leave content empty.
/// When the content extraction yields nothing usable, scan the reasoning text for
/// csharp blocks and return candidates ranked "most likely to be the final code".
/// Filters out placeholder/draft blocks so we don't render partial scaffolding.
let private extractCodeCandidatesFromReasoning (reasoning: string) : string list =
    if String.IsNullOrWhiteSpace reasoning then []
    else
        let rx =
            System.Text.RegularExpressions.Regex(
                @"```(?:csharp|cs)\s*\n(?<code>[\s\S]*?)```",
                System.Text.RegularExpressions.RegexOptions.Compiled)
        let blocks =
            [ for m in rx.Matches(reasoning) -> m.Groups.["code"].Value.Trim() ]
        let isPlaceholderApp (block: string) =
            let m =
                System.Text.RegularExpressions.Regex.Match(
                    block, @"^//\s*app:\s*(.+?)\s*$",
                    System.Text.RegularExpressions.RegexOptions.Multiline)
            if not m.Success then true
            else
                let name = m.Groups.[1].Value.Trim()
                name = "..." || name = "MyAppName" || name = "MyApp" || name = "TODO"
                || not (System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9]*$"))
        blocks
        |> List.filter (fun b ->
            not (isPlaceholderApp b)
            && b.Length >= 400
            && b.Contains "var scene")
        |> List.sortByDescending String.length

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
// Protocol logging — thin wrapper over RunLogger
// ---------------------------------------------------------------------------
//
// ProtocolLog used to own a StreamWriter and a directory; now RunLogger owns
// all file handles centrally (workflow.log, raw.jsonl, per-step files). This
// type just carries the run directory for callers that need to drop .cs/.gif
// artifacts next to the log.

let private outputDir = Path.Combine(projectDir, "output")

type private ProtocolLog = { Dir: string }

let private startProtocol (issueNumber: int) (issueTitle: string) (configName: string) =
    let runDir = RunLogger.startRun outputDir issueNumber issueTitle configName
    { Dir = runDir }

let private log (_: ProtocolLog) (role: string) (text: string) =
    RunLogger.writeNote (sprintf "[%s]\n%s" role text)

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

/// Renders a table of per-call metrics collected during one comment's worth of AI work.
/// `entries` is (roleLabel, stats) pairs — one row per askChatEx call.
let private metricsMarkdown (entries: (string * CallStats) list) =
    let entries = entries |> List.filter (fun (_, s) -> s.Metrics.IsSome || s.TextChars > 0 || s.ThinkingChars > 0)
    if entries.IsEmpty then ""
    else
        let fmtDur (ns: int64) = $"{float ns / 1e9:F1}s"
        let row (label: string, s: CallStats) =
            match s.Metrics with
            | Some m ->
                let genSpeed =
                    if m.EvalDurNs > 0L then $"{float m.EvalCount / (float m.EvalDurNs / 1e9):F1} tok/s"
                    else "—"
                $"| {label} | {m.PromptEvalCount} | {m.EvalCount} | {s.ThinkingChars} | {s.TextChars} | {fmtDur m.PromptEvalDurNs} | {genSpeed} | {fmtDur m.TotalDurNs} |"
            | None ->
                $"| {label} | — | — | {s.ThinkingChars} | {s.TextChars} | — | — | — |"
        let sumMetrics =
            entries
            |> List.choose (fun (_, s) -> s.Metrics)
            |> List.fold (fun acc m ->
                { PromptEvalCount = acc.PromptEvalCount + m.PromptEvalCount
                  EvalCount = acc.EvalCount + m.EvalCount
                  PromptEvalDurNs = acc.PromptEvalDurNs + m.PromptEvalDurNs
                  EvalDurNs = acc.EvalDurNs + m.EvalDurNs
                  TotalDurNs = acc.TotalDurNs + m.TotalDurNs })
                { PromptEvalCount = 0L; EvalCount = 0L; PromptEvalDurNs = 0L; EvalDurNs = 0L; TotalDurNs = 0L }
        let totalThink = entries |> List.sumBy (fun (_, s) -> s.ThinkingChars)
        let totalText = entries |> List.sumBy (fun (_, s) -> s.TextChars)
        let totalRow =
            let genSpeed =
                if sumMetrics.EvalDurNs > 0L then $"{float sumMetrics.EvalCount / (float sumMetrics.EvalDurNs / 1e9):F1} tok/s"
                else "—"
            $"| **Σ** | **{sumMetrics.PromptEvalCount}** | **{sumMetrics.EvalCount}** | **{totalThink}** | **{totalText}** | **{fmtDur sumMetrics.PromptEvalDurNs}** | **{genSpeed}** | **{fmtDur sumMetrics.TotalDurNs}** |"
        let header =
            "| Role | Prompt tok | Gen tok | Think chars | Text chars | Prefill | Gen speed | Total |\n" +
            "|------|-----------:|--------:|------------:|-----------:|--------:|----------:|------:|"
        let body = (entries |> List.map row) @ [ totalRow ] |> String.concat "\n"
        $"\n\n<details>\n<summary>Run Metrics</summary>\n\n{header}\n{body}\n\n</details>"

// ---------------------------------------------------------------------------
// Code extraction & rendering
// ---------------------------------------------------------------------------

let private renderPixogram (config: PipelineConfig) (csPath: string) (gifPath: string) : Result<string, string> =
    printfn $"  Rendering: {Path.GetFileName csPath} → {Path.GetFileName gifPath}"
    let psi = ProcessStartInfo "Pxl.Render"
    // Fixed virtual start time so previews always show a minute rollover
    // (13:05:50 + 30s window = renders 13:05 → 13:06 transition)
    for a in [ csPath; "--output"; gifPath; "--duration"; string config.GifDurationSeconds; "--scale"; string config.GifScale; "--mode"; "clock"; "--start-time"; "13:05:50" ] do
        psi.ArgumentList.Add a
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.UseShellExecute <- false
    psi.CreateNoWindow <- true
    try
        use proc = Process.Start psi
        // Async stream reads to avoid pipe-deadlock when child spams stderr
        let out = System.Text.StringBuilder()
        let err = System.Text.StringBuilder()
        proc.OutputDataReceived.Add(fun e -> if isNull e.Data |> not then out.AppendLine e.Data |> ignore)
        proc.ErrorDataReceived.Add(fun e -> if isNull e.Data |> not then err.AppendLine e.Data |> ignore)
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()
        // Pxl.Render is a simulation — complex scenes with many objects can render
        // ~10× slower than wall-clock. 5 min is a safety-net for infinite loops; real
        // crashes abort via Pxl.Render's fail-fast (0.0.68+) in a second or two.
        let renderTimeoutMs = 300_000
        let exited = proc.WaitForExit(renderTimeoutMs)
        if not exited then
            try proc.Kill(true) with _ -> ()
            printfn $"  [{ts ()}] Render TIMEOUT (300s)"
            Result.Error "Render timed out after 300s"
        elif proc.ExitCode = 0 then
            let fileSize = FileInfo(gifPath).Length / 1024L
            printfn $"  [{ts ()}] Render OK ({fileSize} KB)"
            Ok gifPath
        else
            let output = (out.ToString() + "\n" + err.ToString()).Trim()
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
    // Per-process suffix so parallel workflow runs don't collide on the same worktree.
    let worktreePath =
        Path.Combine(Path.GetTempPath(), $"pixogram-maker-wt-{System.Diagnostics.Process.GetCurrentProcess().Id}")
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
            |> List.chunkBySize 2
            |> List.map (fun chunk ->
                let cells = chunk |> List.map cell
                let padded = cells @ List.replicate (2 - cells.Length) ""
                "| " + (padded |> String.concat " | ") + " |")
            |> String.concat "\n"
        let header = "|  |  |\n|:-:|:-:|"
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
    match askChat "Compaction" config.Models.Compaction config.AiTimeoutMs messages with
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
        match callAgentEx (sprintf "%s (attempt %d/%d)" label attempt config.MaxDirectorRetries) backend config.AiTimeoutMs promptFile conversationMessages with
        | Result.Error err ->
            printfn $"  ✗ {label} failed: {err}"
            log protocol label $"FAILED (attempt {attempt}): {err}"
        | Ok (response, _) when not (response.Contains(expectedTag)) ->
            printfn $"  ✗ {label} response missing expected tag '{expectedTag}', discarding."
            log protocol label $"DISCARDED (attempt {attempt}, missing tag): {response}"
        | Ok (response, stats) ->
            log protocol label response
            printfn $"    Posting comment..."
            let footer = metricsMarkdown [ label, stats ]
            postComment issueNumber (response + footer)
            printfn $"  ✓ {label} posted."
            posted <- true
        attempt <- attempt + 1
    if not posted then
        printfn $"  ✗ {label} failed after {config.MaxDirectorRetries} attempts."

let private generateSummary (config: PipelineConfig) (conversationMessages: ChatMessage list) : string * CallStats =
    printfn $"    [{ts ()}] Generating summary..."
    let stripped = conversationMessages |> sliceForSummary |> stripDetailsFromMessages
    let conversationText = renderConversationAsText stripped
    let prompt = renderPrompt "summary.md" [ "conversation", conversationText ]
    let messages = [ ChatMessage.system noToolsPrompt; ChatMessage.user prompt ]
    match askChatEx "Summary" config.Models.Triage config.AiTimeoutMs messages with
    | Ok (summary, stats) -> summary.Trim(), stats
    | Result.Error err ->
        printfn $"    ✗ Summary failed: {err}"
        "", emptyCallStats

let private executeImplementor (config: PipelineConfig) (protocol: ProtocolLog) (conversationMessages: ChatMessage list) (fullConversationMessages: ChatMessage list) (comments: IssueComment list) (issueNumber: int) (issueTitle: string) : bool =
    printfn $"  [{ts ()}] ▶ Running Implementor ({backendDisplayName config.Models.Implementor})..."
    printfn "    Prompt: implementor.md"

    let iterationNumber = countImplementorComments comments + 1
    printfn $"    Iteration: #{iterationNumber}"

    let implTokens = conversationMessages |> renderConversationAsText |> estimateTokens
    printfn $"    Implementor conversation: ~{implTokens} tokens"

    let systemPrompt = renderSystemPrompt "implementor.md" [ "api_reference", apiReference.Value ]
    let baseMessages = ChatMessage.system systemPrompt :: conversationMessages

    // Accept the response if it either carries the expected `// ---` frontmatter OR
    // structurally looks like C# code (braces, semicolons, and at least one Pxl API
    // reference). The frontmatter is the strict contract, but small models often drop
    // it while still producing valid code — rendering will catch truly broken output
    // via the retry loop, which is cheaper than a think-fallback round-trip.
    let looksLikeCSharp (s: string) =
        s.Length > 300
        && s.Contains '{' && s.Contains '}' && s.Contains ';'
        && (s.Contains "Renderer" || s.Contains "ctx." || s.Contains "void Frame"
            || s.Contains "DrawingContext" || s.Contains "RenderCtx")

    let metricsLog = ResizeArray<string * CallStats>()

    let validate (code: string) =
        let hasMarker = code.Contains("// ---")
        let looksLikeCode = looksLikeCSharp code
        if hasMarker then Some "marker"
        elif looksLikeCode then Some "heuristic"
        else None

    /// Try the primary content first; if it doesn't validate, fall back to candidates
    /// extracted from the reasoning stream (some providers dump everything there).
    let pickValidCode (response: string) (thinking: string) : (string * string) option =
        let primary = extractCodeFromMarkdown response
        match validate primary with
        | Some via -> Some (primary, via)
        | None ->
            extractCodeCandidatesFromReasoning thinking
            |> List.tryPick (fun cand ->
                match validate cand with
                | Some via -> Some (cand, sprintf "reasoning-recovery/%s" via)
                | None -> None)

    let getInitialCode (backend: SelectedBackend) (label: string) =
        printfn $"    [{ts ()}] [impl] Sending to {backendDisplayName backend}..."
        match askChatEx label backend config.AiTimeoutMs baseMessages with
        | Ok (response, stats) ->
            metricsLog.Add((label, stats))
            match pickValidCode response stats.Thinking with
            | Some (code, via) ->
                printfn $"    [impl] Code extracted via {via} ({code.Length} chars from {response.Length} chars response, {stats.Thinking.Length} chars reasoning)"
                if via.StartsWith "reasoning-recovery" then
                    log protocol "Implementor" $"RECOVERED from reasoning ({code.Length} chars; content was empty/unusable)"
                Some code
            | None ->
                let snippet =
                    let preview = extractCodeFromMarkdown response
                    if preview.Length = 0 then "(empty)"
                    else preview.[..min 200 (preview.Length - 1)]
                printfn $"  ⚠ Implementor response is not C# code (response {response.Length} chars, reasoning {stats.Thinking.Length} chars)"
                log protocol "Implementor" $"INVALID (no marker, not code-like, no reasoning fallback): {snippet}"
                None
        | Result.Error err ->
            printfn $"  ✗ Implementor AI failed: {err}"
            log protocol "Implementor" $"FAILED: {err}"
            None

    let initialCode =
        match getInitialCode config.Models.Implementor "Implementor (initial)" with
        | Some code -> Some code
        | None ->
            match config.Models.ImplementorFallback with
            | Some fallback ->
                printfn $"  ↩ Trying fallback model ({backendDisplayName fallback})..."
                log protocol "Implementor" $"FALLBACK: switching to {backendDisplayName fallback}"
                getInitialCode fallback "Implementor (fallback)"
            | None ->
                printfn $"  ✗ No fallback model configured."
                None

    match initialCode with
    | None ->
        printfn $"  ✗ Implementor could not produce valid code."
        log protocol "Implementor" "GAVE UP: no valid code from primary or fallback"
        false
    | Some initialCode ->

    // Render-retry loop. Append-only history: every attempt's [code, error] pair
    // stays in the prompt across subsequent retries. This keeps the prompt prefix
    // byte-identical between attempts so Ollama's KV-cache reuses the slot
    // (Attempt N's prefix is fully a cache hit on Attempt N+1). Trimming to only
    // "last code + last error" would be shorter but would invalidate the cache
    // slot on every retry — smaller prompt, but far more tokens actually computed.
    let mutable code = initialCode
    let mutable attempt = 1
    let mutable success = false
    let retryHistory = ResizeArray<ChatMessage>()

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
            // Include the just-produced code so the summary describes THIS iteration,
            // not the previous one. Tag it so sliceForSummary recognises it as the latest Implementor.
            let latestImplMessage =
                ChatMessage.assistant $"**[Implementor]** — Iteration {iterationNumber}\n\n<details>\n\n```csharp\n{code}\n```\n\n</details>"
            let summaryConversation = fullConversationMessages @ [ latestImplMessage ]
            let summary, summaryStats = generateSummary config summaryConversation
            metricsLog.Add(("Summary", summaryStats))
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
                configSetMarkdown config.Models +
                metricsMarkdown (List.ofSeq metricsLog)
            postComment issueNumber comment
            printfn $"  [{ts ()}] ✓ Implementor posted — iteration {iterationNumber} (attempt {attempt})."
            updateIssueGallery issueNumber issueTitle
            success <- true

        | Result.Error err ->
            printfn $"    Render failed (attempt {attempt}): {err}"
            log protocol "Render" $"ATTEMPT {attempt} FAILED: {err}"

            if attempt < config.MaxImplementorRetries then
                let feedback =
                    "The previous attempt failed to compile/render with this error:\n\n" +
                    $"```\n{err}\n```\n\n" +
                    "Focus on fixing THIS error. Output the corrected complete C# code in a ```csharp block."
                // Append this attempt's [code, feedback] to the retry history. Prior
                // attempts stay in the prompt — keeps the prefix stable for KV-cache hits.
                retryHistory.Add(ChatMessage.assistant code)
                retryHistory.Add(ChatMessage.user feedback)
                let retryMessages = baseMessages @ List.ofSeq retryHistory
                printfn $"    [impl] Sending error feedback (attempt {attempt}, history: {retryHistory.Count / 2} pairs)..."
                match askChatEx (sprintf "Implementor (retry %d)" attempt) config.Models.Implementor config.AiTimeoutMs retryMessages with
                | Result.Error aiErr ->
                    printfn $"  ✗ Implementor retry AI failed: {aiErr}"
                    log protocol "Implementor" $"RETRY AI FAILED: {aiErr}"
                    attempt <- config.MaxImplementorRetries // bail out
                | Ok (response, stats) ->
                    metricsLog.Add(($"Implementor (retry {attempt})", stats))
                    let fixedCode, via =
                        match pickValidCode response stats.Thinking with
                        | Some (c, v) -> c, v
                        | None -> extractCodeFromMarkdown response, "raw"
                    if via.StartsWith "reasoning-recovery" then
                        log protocol "Implementor" $"RETRY {attempt}: RECOVERED from reasoning ({fixedCode.Length} chars)"
                    printfn $"    [impl] Retry code extracted via {via} ({fixedCode.Length} chars from {response.Length} chars response, {stats.Thinking.Length} chars reasoning)"
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
    let protocol = startProtocol issue.Number issue.Title config.Models.Name
    try
        if runSafetyGate config protocol issue then
            runApprovalGate config protocol issue |> ignore
    finally
        RunLogger.stopRun ()

let run (config: PipelineConfig) (issue: Issue) =
    let protocol = startProtocol issue.Number issue.Title config.Models.Name

    try
        if not (runSafetyGate config protocol issue) then
            printfn "  ─── Workflow aborted (not a pixogram request) ───"
        elif not (runApprovalGate config protocol issue) then
            printfn "  ─── Workflow aborted (not approved) ───"
        else

        // Auto-apply the model-config label so GitHub shows which ConfigSet produced
        // this iteration. Best-effort: missing label on the repo is not fatal.
        let modelLabel = "model-" + config.Models.Name
        if not (issue.Labels |> List.exists (fun l -> String.Equals(l, modelLabel, StringComparison.OrdinalIgnoreCase))) then
            match tryAddLabel issue.Number modelLabel with
            | Result.Ok () -> printfn $"  Applied label: {modelLabel}"
            | Result.Error msg -> printfn $"  ⚠ Could not apply label '{modelLabel}' (skipping): {msg}"

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
                        let wasCapped = newMax < desired
                        let cappedNote = if wasCapped then $" (capped by MAX_ITERATIONS_CAP={config.MaxIterationsCap})" else ""
                        if newMax > maxIterations then
                            printfn $"  ⤴ User requested +{bump} auto-iterations → maxIterations: {maxIterations} → {newMax}{cappedNote}"
                            log protocol "Workflow" $"User bumped maxIterations: {maxIterations} → {newMax} (+{bump}, impl={implCount}){cappedNote}"
                            maxIterations <- newMax
                            if wasCapped then
                                let granted = newMax - implCount
                                let comment =
                                    "<!-- pixogram-bot-note -->\n" +
                                    $"Requested +{bump} auto-iterations, but capped to +{granted} by `MAX_ITERATIONS_CAP={config.MaxIterationsCap}` " +
                                    $"(current: {implCount}/{newMax}). Comment again with another bump to continue past the cap."
                                postComment current.Number comment
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

            // Build conversations:
            //   - Narrative: code-stripped transcript for analytical roles (Triage, Directors)
            //   - Full: chat-message form, used for Summary
            //   - Implementor: chat-message form filtered to the current cycle (for code generation)
            let narrativeConversation = buildConversation config ConversationView.Narrative compaction current
            let fullConversation = buildConversation config ConversationView.Full compaction current
            let implConversation = buildConversation config ConversationView.Implementor compaction current
            let narrativeTokens = narrativeConversation |> renderConversationAsText |> estimateTokens
            printfn $"  Narrative conversation: ~{narrativeTokens} tokens (code-stripped transcript for Triage + Directors)"
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
                        // Narrative transcript: code stripped, chronological, role-labeled.
                        // Triage reasons about WHO said WHAT in which order, so this view is
                        // clearer than the chat-message form with code omitted per-turn.
                        let triageAction = determineNextAction config maxIterations current.Author narrativeConversation
                        log protocol "Triage" $"{triageAction}"
                        lastTriageCommentCount <- current.Comments.Length
                        lastTriageResult <- Some triageAction
                        triageAction
            printfn ""

            match action with
            | RunVisionary ->
                executeDirector config protocol config.Models.DirectorVisionary "director-visionary.md" "Director/Visionary" narrativeConversation current.Number
            | RunMaverick ->
                executeDirector config protocol config.Models.DirectorMaverick "director-maverick.md" "Director/Maverick" narrativeConversation current.Number
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
        RunLogger.stopRun ()

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
