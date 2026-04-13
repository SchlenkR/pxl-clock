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
// Issue → conversation string (delegates to Conversation module)
// ---------------------------------------------------------------------------

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

let private renderPixogram (csPath: string) (gifPath: string) : Result<string, string> =
    printfn $"  Rendering: {Path.GetFileName csPath} → {Path.GetFileName gifPath}"
    let psi = ProcessStartInfo "Pxl.Render"
    for a in [ csPath; "--output"; gifPath; "--duration"; string gifDurationSeconds; "--scale"; string gifScale; "--mode"; "clock" ] do
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

let private releaseTag (issueNumber: int) = $"pixogram-issue-{issueNumber}"

let private ensureRelease (issueNumber: int) =
    let tag = releaseTag issueNumber
    match runGh [ "release"; "view"; tag ] with
    | Some _ -> ()
    | None ->
        printfn $"    Creating release '{tag}'..."
        runGh [ "release"; "create"; tag; "--title"; $"Pixogram Issue #{issueNumber}"; "--notes"; $"Rendered GIFs for issue #{issueNumber}"; "--latest=false" ] |> ignore

let private uploadGif (gifPath: string) (issueNumber: int) : string option =
    ensureRelease issueNumber
    let tag = releaseTag issueNumber
    let assetName = Path.GetFileName gifPath
    printfn $"    Uploading {assetName} to release '{tag}'..."
    runGh [ "release"; "upload"; tag; gifPath; "--clobber" ] |> ignore
    Some $"https://github.com/{owner}/{repoName}/releases/download/{tag}/{assetName}"

// ---------------------------------------------------------------------------
// Step execution
// ---------------------------------------------------------------------------

let private executeDirector (protocol: ProtocolLog) (backend: SelectedBackend) (promptFile: string) (label: string) (conversation: string) (issueNumber: int) =
    printfn $"  ▶ Running {label} ({backendDisplayName backend})..."
    printfn $"    Prompt: {promptFile}"
    match callAgent backend promptFile conversation with
    | Error err ->
        printfn $"  ✗ {label} failed: {err}"
        log protocol label $"FAILED: {err}"
    | Ok response ->
        log protocol label response
        printfn $"    Posting comment..."
        postComment issueNumber response
        printfn $"  ✓ {label} posted."

let private executeCraftsman (protocol: ProtocolLog) (conversation: string) : string option =
    printfn $"  ▶ Running Craftsman ({backendDisplayName Backends.craftsman})..."
    printfn $"    Prompt: director-craftsman.md"
    match callAgent Backends.craftsman "director-craftsman.md" conversation with
    | Error err ->
        printfn $"  ✗ Craftsman failed: {err}"
        log protocol "Craftsman" $"FAILED: {err}"
        None
    | Ok response ->
        log protocol "Craftsman" response
        printfn $"  ✓ Craftsman done (not posting, will embed in Implementor comment)."
        Some response

let private generateSummary (conversation: string) =
    printfn "    Generating summary..."
    match askAI Backends.triage (renderPrompt "summary.md" [ "conversation", conversation ]) with
    | Ok summary -> summary.Trim()
    | Error err ->
        printfn $"    ✗ Summary failed: {err}"
        ""

let private executeImplementor (protocol: ProtocolLog) (conversation: string) (fullConversation: string) (craftsmanText: string option) (comments: IssueComment list) (issueNumber: int) =
    printfn $"  ▶ Running Implementor ({backendDisplayName Backends.implementor})..."
    printfn "    Prompt: implementor.md"

    let iterationNumber = countImplementorComments comments + 1
    printfn $"    Iteration: #{iterationNumber}"

    let prompt = renderPrompt "implementor.md" [ "conversation", conversation ]

    use agent = createAgent Backends.implementor
    printfn $"    [impl] Agent created, sending initial prompt..."

    // Step 1: Get initial code
    match sendToAgent agent prompt with
    | Error err ->
        printfn $"  ✗ Implementor AI failed: {err}"
        log protocol "Implementor" $"FAILED: {err}"
    | Ok initialCode ->

    let mutable code = initialCode
    let mutable attempt = 1
    let mutable success = false

    while not success && attempt <= maxImplementorRetries do
        printfn $"    [impl] Render attempt {attempt}/{maxImplementorRetries}..."
        log protocol "Implementor" $"ATTEMPT {attempt}:\n{code}"

        let timestamp = DateTime.Now.ToString "yyyy-MM-dd_HH-mm-ss"
        let csPath = Path.Combine(protocol.Dir, $"{timestamp}.cs")
        let gifPath = Path.Combine(protocol.Dir, $"{timestamp}.gif")
        File.WriteAllText(csPath, code)
        printfn $"    Code saved: {csPath}"

        match renderPixogram csPath gifPath with
        | Ok _ ->
            printfn $"    Render OK"
            log protocol "Render" $"OK: {gifPath}"
            let gifUrl = uploadGif gifPath issueNumber
            let gifMarkdown =
                gifUrl
                |> Option.map (fun url -> $"\n\n![preview]({url})")
                |> Option.defaultValue ""
            let summary = generateSummary fullConversation
            let summaryLine = if summary <> "" then $"\n\n{summary}" else ""
            let craftsmanBlock =
                match craftsmanText with
                | Some ct ->
                    $"\n\n<details>\n<summary>Craftsman Specification</summary>\n\n" +
                    $"{ct}\n\n</details>"
                | None -> ""
            let comment =
                $"{roleTag Role.Implementor} — Iteration #{iterationNumber}" +
                summaryLine +
                gifMarkdown +
                craftsmanBlock +
                $"\n\n<details>\n<summary>Code anzeigen</summary>\n\n" +
                $"```csharp\n{code}\n```\n\n</details>"
            postComment issueNumber comment
            printfn $"  ✓ Implementor posted — iteration #{iterationNumber} (attempt {attempt})."
            success <- true

        | Error err ->
            printfn $"    Render failed (attempt {attempt}): {err}"
            log protocol "Render" $"ATTEMPT {attempt} FAILED: {err}"

            if attempt < maxImplementorRetries then
                let feedback =
                    "The code failed to compile/render. Here is the error:\n\n" +
                    $"```\n{err}\n```\n\n" +
                    "Please fix the code and output ONLY the corrected raw C# code. No markdown, no explanations."
                printfn $"    [impl] Sending error feedback to same session..."
                match sendToAgent agent feedback with
                | Error aiErr ->
                    printfn $"  ✗ Implementor retry AI failed: {aiErr}"
                    log protocol "Implementor" $"RETRY AI FAILED: {aiErr}"
                    attempt <- maxImplementorRetries // bail out
                | Ok fixedCode ->
                    code <- fixedCode

        attempt <- attempt + 1

    if not success then
        printfn $"  ✗ Implementor failed after {attempt - 1} attempts, not posting to GitHub."
        log protocol "Implementor" $"GAVE UP after {attempt - 1} attempts"

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

let private runApprovalGate (protocol: ProtocolLog) (issue: Issue) : bool =
    if hasLabel issue.Number labelApproved then
        printfn $"  ✓ Issue #{issue.Number} already approved."
        true
    elif isMaintainer issue.Author then
        printfn $"  ✓ Issue #{issue.Number} auto-approved (author {issue.Author} is maintainer)."
        addLabel issue.Number labelApproved
        log protocol "Approval" $"Auto-approved ({issue.Author} is maintainer)"
        true
    else
        let names = String.Join(", ", maintainers |> List.map (fun m -> $"@{m}"))
        printfn $"  ✗ Issue #{issue.Number} needs approval from a maintainer."
        postComment issue.Number $"{names} Bitte gebt dieses Issue frei (Label `{labelApproved}` setzen)."
        log protocol "Approval" "Waiting for maintainer approval"
        false

let private runSafetyGate (protocol: ProtocolLog) (issue: Issue) : bool =
    if hasLabel issue.Number labelIgnore then
        printfn $"  ✗ Issue #{issue.Number} has '{labelIgnore}' label — skipping."
        log protocol "Safety" "Skipped: marked as ignore."
        false
    elif hasLabel issue.Number labelTriagePassed then
        printfn $"  ✓ Issue #{issue.Number} already has '{labelTriagePassed}' label."
        true
    else
        printfn $"  Running safety check for issue #{issue.Number}..."
        match runSafetyCheck issue with
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

let triageOnly (issue: Issue) =
    let protocol = startProtocol issue.Number
    try
        if runApprovalGate protocol issue then
            runSafetyGate protocol issue |> ignore
    finally
        protocol.Writer.Dispose()

let run (issue: Issue) =
    let protocol = startProtocol issue.Number

    try
        if not (runApprovalGate protocol issue) then
            printfn "  ─── Workflow aborted (not approved) ───"
        elif not (runSafetyGate protocol issue) then
            printfn "  ─── Workflow aborted (safety check failed) ───"
        else

        let maxIterations = extractIterationCount issue.Body
        printfn $"  Max iterations: {maxIterations}"

        let mutable running = true
        while running do
            printfn ""
            printfn "  ─── Fetching issue state... ───"
            let current = fetchIssueWithComments issue
            let fullConversation = buildConversation ConversationView.Full current
            let implConversation = buildConversation ConversationView.Implementor current
            let implCount = countImplementorComments current.Comments
            printfn $"  Issue #{current.Number}: {current.Title}"
            printfn $"  Comments: {current.Comments.Length}, Implementor iterations: {implCount}/{maxIterations}"
            printfn ""

            if implCount >= maxIterations then
                printfn $"  Max iterations reached ({implCount}/{maxIterations}) — stopping."
                log protocol "Workflow" $"Max iterations reached ({implCount}/{maxIterations})"
                running <- false
            else

            let action = determineNextAction maxIterations current.Author fullConversation
            log protocol "Triage" $"{action}"
            printfn ""

            match action with
            | RunVisionary ->
                executeDirector protocol Backends.directorVisionary "director-visionary.md" "Director/Visionary" fullConversation current.Number
            | RunMaverick ->
                executeDirector protocol Backends.directorMaverick "director-maverick.md" "Director/Maverick" fullConversation current.Number
            | RunCraftsman ->
                match executeCraftsman protocol fullConversation with
                | None -> ()
                | Some craftsmanResponse ->
                    let craftsmanComment =
                        $"\n<comment id=\"0\" author=\"pipeline\" role=\"craftsman\" time=\"{DateTime.UtcNow:O}\">\n" +
                        $"<![CDATA[{craftsmanResponse}]]>\n" +
                        "</comment>\n"
                    // Insert craftsman comment before </conversation> closing tag
                    let extendedConversation =
                        implConversation.Replace("</conversation>", craftsmanComment + "</conversation>")
                    executeImplementor protocol extendedConversation fullConversation (Some craftsmanResponse) current.Comments current.Number
            | RunImplementor ->
                executeImplementor protocol implConversation fullConversation None current.Comments current.Number
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
