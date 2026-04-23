module PixogramRequests.RunLogger

open System
open System.IO
open System.Text
open System.Text.Json
open AiBase.Agent
open AiBase.AgentSelection

// ---------------------------------------------------------------------------
// Purpose
// ---------------------------------------------------------------------------
//
// One run → one directory on disk containing:
//   workflow.log   — human-readable narrative, top-to-bottom
//   raw.jsonl      — every RawRequest/RawEvent verbatim, one JSON per line
//   steps/NNN-<name>/
//       request.json    — the outgoing message payload (messages + metadata)
//       raw-request.txt — the exact bytes the agent sent
//       thinking.txt    — all Thinking tokens of this step, in order
//       response.txt    — all Text tokens of this step, in order
//       raw.jsonl       — this step's raw events only
//
// Design: module-level mutable state (single-threaded workflow, no contention).
// Step names come from the caller — they know the semantic role best.
// If no step is active, events still land in run-level files so nothing is dropped.
// ---------------------------------------------------------------------------

type private Step =
    {
        Index: int
        Name: string
        StartedAt: DateTime
        Dir: string
        RawWriter: StreamWriter
        ThinkingWriter: StreamWriter
        TextWriter: StreamWriter
        mutable RequestLogged: bool
        mutable ThinkingChars: int
        mutable TextChars: int
        // Metrics arrive mid-stream but should render AFTER thinking+response in the
        // narrative. Stash here, dump via logMetricsTail at the end.
        mutable LastMetrics: CallMetrics option
    }

type private Run =
    {
        IssueNumber: int
        IssueTitle: string
        ConfigName: string
        StartedAt: DateTime
        RunDir: string
        WorkflowWriter: StreamWriter
        RawWriter: StreamWriter
        mutable StepCounter: int
        mutable CurrentStep: Step option
    }

let mutable private current: Run option = None

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private CR = '\r'

let private trimCR (s: string) = s.TrimEnd(CR)

let private fmtTimestamp (d: DateTime) = d.ToString("yyyy-MM-dd HH:mm:ss")

let private fmtDuration (ts: TimeSpan) =
    let h = int ts.TotalHours
    let m = ts.Minutes
    let s = ts.Seconds
    sprintf "%02d:%02d:%02d" h m s

let private sanitizeSegment (s: string) =
    let pick (c: char) =
        if Char.IsLetterOrDigit c then c
        elif c = ' ' || c = '-' || c = '_' then '-'
        else '-'
    let cleaned = s.ToLowerInvariant() |> Seq.map pick |> Seq.toArray |> String
    let collapsed = System.Text.RegularExpressions.Regex.Replace(cleaned, "-+", "-")
    collapsed.Trim('-')

let runDir () = current |> Option.map (fun r -> r.RunDir)

// ---------------------------------------------------------------------------
// Writers — thin wrappers, always flushing so a crash preserves what we had
// ---------------------------------------------------------------------------

let private writeRaw (jsonLine: string) =
    match current with
    | Some r ->
        r.RawWriter.WriteLine(jsonLine)
        r.RawWriter.Flush()
        match r.CurrentStep with
        | Some s ->
            s.RawWriter.WriteLine(jsonLine)
            s.RawWriter.Flush()
        | None -> ()
    | None -> ()

// ---------------------------------------------------------------------------
// JSON helpers for raw.jsonl
// ---------------------------------------------------------------------------

let private jsonEscape (s: string) = JsonSerializer.Serialize(s)

let private rawEntry (direction: string) (kind: string) (stepName: string option) (payload: string) =
    let sb = StringBuilder()
    let ts = jsonEscape (DateTime.Now.ToString("O"))
    sb.Append("{\"ts\":") |> ignore
    sb.Append(ts) |> ignore
    sb.Append(",\"direction\":\"") |> ignore
    sb.Append(direction) |> ignore
    sb.Append("\",\"kind\":\"") |> ignore
    sb.Append(kind) |> ignore
    sb.Append("\"") |> ignore
    match stepName with
    | Some n ->
        sb.Append(",\"step\":") |> ignore
        sb.Append(jsonEscape n) |> ignore
    | None -> ()
    sb.Append(",\"payload\":") |> ignore
    sb.Append(jsonEscape payload) |> ignore
    sb.Append("}") |> ignore
    sb.ToString()

// ---------------------------------------------------------------------------
// Run lifecycle
// ---------------------------------------------------------------------------

let startRun (outputBaseDir: string) (issueNumber: int) (issueTitle: string) (configName: string) : string =
    let now = DateTime.Now
    let timestamp = now.ToString("yyyy-MM-dd_HH-mm-ss")
    let configSlug = sanitizeSegment configName
    let issueDir = Path.Combine(outputBaseDir, sprintf "issue-%d" issueNumber)
    let runsDir = Path.Combine(issueDir, "runs")
    let runDir = Path.Combine(runsDir, sprintf "%s_%s" timestamp configSlug)
    Directory.CreateDirectory(runDir) |> ignore
    Directory.CreateDirectory(Path.Combine(runDir, "steps")) |> ignore

    let wf = new StreamWriter(Path.Combine(runDir, "workflow.log"), append = false, encoding = Encoding.UTF8)
    wf.AutoFlush <- true
    let raw = new StreamWriter(Path.Combine(runDir, "raw.jsonl"), append = false, encoding = Encoding.UTF8)
    raw.AutoFlush <- true

    let run =
        {
            IssueNumber = issueNumber
            IssueTitle = issueTitle
            ConfigName = configName
            StartedAt = now
            RunDir = runDir
            WorkflowWriter = wf
            RawWriter = raw
            StepCounter = 0
            CurrentStep = None
        }
    current <- Some run

    let title = if String.IsNullOrWhiteSpace issueTitle then "(no title)" else issueTitle
    let startedStr = fmtTimestamp now
    let bar = String.replicate 80 "="
    wf.WriteLine("")
    wf.WriteLine(bar)
    wf.WriteLine(sprintf "  WORKFLOW  -  Issue #%d  \"%s\"" issueNumber title)
    wf.WriteLine(sprintf "  Config:   %s" configName)
    wf.WriteLine(sprintf "  Started:  %s" startedStr)
    wf.WriteLine(sprintf "  Run dir:  %s" runDir)
    wf.WriteLine(bar)
    wf.WriteLine("")
    runDir

let stopRun () : unit =
    match current with
    | Some r ->
        match r.CurrentStep with
        | Some s ->
            s.RawWriter.Dispose()
            s.ThinkingWriter.Dispose()
            s.TextWriter.Dispose()
        | None -> ()
        let now = DateTime.Now
        let duration = now - r.StartedAt
        let endStr = fmtTimestamp now
        let durStr = fmtDuration duration
        let bar = String.replicate 80 "="
        r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.WriteLine(bar)
        r.WorkflowWriter.WriteLine(sprintf "  FINISHED  at %s  (duration: %s)" endStr durStr)
        r.WorkflowWriter.WriteLine(bar)
        r.WorkflowWriter.Flush()
        r.WorkflowWriter.Dispose()
        r.RawWriter.Dispose()
        current <- None
    | None -> ()

let isActive () = current.IsSome

// ---------------------------------------------------------------------------
// Step lifecycle
// ---------------------------------------------------------------------------

let beginStep (name: string) : unit =
    match current with
    | None -> ()
    | Some r ->
        match r.CurrentStep with
        | Some s ->
            s.RawWriter.Dispose()
            s.ThinkingWriter.Dispose()
            s.TextWriter.Dispose()
        | None -> ()

        r.StepCounter <- r.StepCounter + 1
        let idx = r.StepCounter
        let slug = sanitizeSegment name
        let stepDirName = sprintf "%03d-%s" idx slug
        let stepDir = Path.Combine(r.RunDir, "steps", stepDirName)
        Directory.CreateDirectory(stepDir) |> ignore
        let rawW = new StreamWriter(Path.Combine(stepDir, "raw.jsonl"), append = false, encoding = Encoding.UTF8)
        rawW.AutoFlush <- true
        let thinkW = new StreamWriter(Path.Combine(stepDir, "thinking.txt"), append = false, encoding = Encoding.UTF8)
        thinkW.AutoFlush <- true
        let textW = new StreamWriter(Path.Combine(stepDir, "response.txt"), append = false, encoding = Encoding.UTF8)
        textW.AutoFlush <- true

        let step =
            {
                Index = idx
                Name = name
                StartedAt = DateTime.Now
                Dir = stepDir
                RawWriter = rawW
                ThinkingWriter = thinkW
                TextWriter = textW
                RequestLogged = false
                ThinkingChars = 0
                TextChars = 0
                LastMetrics = None
            }
        r.CurrentStep <- Some step

        let startedStr = fmtTimestamp step.StartedAt
        let bar = String.replicate 80 "-"
        r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.WriteLine(bar)
        r.WorkflowWriter.WriteLine(sprintf "  STEP %03d  -  %s" idx name)
        r.WorkflowWriter.WriteLine(sprintf "  Started: %s  |  Dir: steps/%s/" startedStr stepDirName)
        r.WorkflowWriter.WriteLine(bar)
        r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.Flush()

// Forward declared — defined below, referenced from endStep as a safety net.
let mutable private logMetricsTailImpl: unit -> unit = fun () -> ()

let endStep () : unit =
    match current with
    | None -> ()
    | Some r ->
        match r.CurrentStep with
        | None -> ()
        | Some s ->
            // Safety net: if the caller forgot to flush metrics explicitly, do it here
            // so nothing is lost. Double-calls are harmless — LastMetrics is set-once.
            logMetricsTailImpl ()
            let now = DateTime.Now
            let dur = now - s.StartedAt
            r.WorkflowWriter.WriteLine("")
            r.WorkflowWriter.WriteLine(
                sprintf "  -> step done in %.1fs  |  thinking %d chars, response %d chars"
                    dur.TotalSeconds s.ThinkingChars s.TextChars)
            r.WorkflowWriter.Flush()
            s.RawWriter.Dispose()
            s.ThinkingWriter.Dispose()
            s.TextWriter.Dispose()
            r.CurrentStep <- None

let withStep (name: string) (fn: unit -> 'T) : 'T =
    beginStep name
    try
        fn ()
    finally
        endStep ()

// ---------------------------------------------------------------------------
// Section writers — for direct workflow.log narration
// ---------------------------------------------------------------------------

let writeSection (header: string) (body: string) : unit =
    match current with
    | None -> ()
    | Some r ->
        r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.WriteLine(sprintf "  v %s" header)
        r.WorkflowWriter.WriteLine("")
        for line in body.Split('\n') do
            r.WorkflowWriter.WriteLine(sprintf "    %s" (trimCR line))
        r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.Flush()

let writeNote (text: string) : unit =
    match current with
    | None -> ()
    | Some r ->
        let ts = DateTime.Now.ToString("HH:mm:ss")
        for line in text.Split('\n') do
            r.WorkflowWriter.WriteLine(sprintf "  [%s] %s" ts (trimCR line))
        r.WorkflowWriter.Flush()

// ---------------------------------------------------------------------------
// Request logging — called from Config.fs askChatEx before SendChat
// ---------------------------------------------------------------------------

let logRequest (backend: SelectedBackend) (messages: ChatMessage list) : unit =
    match current with
    | None -> ()
    | Some r ->
        let backendName = backendDisplayName backend
        let totalChars = messages |> List.sumBy (fun m -> m.Content.Length)

        r.WorkflowWriter.WriteLine(sprintf "  Backend: %s" backendName)
        r.WorkflowWriter.WriteLine(sprintf "  Messages: %d (%d chars total)" messages.Length totalChars)
        r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.WriteLine("  v REQUEST")
        r.WorkflowWriter.WriteLine("")
        for m in messages do
            r.WorkflowWriter.WriteLine(sprintf "    [%s]  (%d chars)" m.Role m.Content.Length)
            for line in m.Content.Split('\n') do
                r.WorkflowWriter.WriteLine(sprintf "    %s" (trimCR line))
            r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.Flush()

        match r.CurrentStep with
        | Some s ->
            let payload =
                let buf = new MemoryStream()
                let w = new Utf8JsonWriter(buf, JsonWriterOptions(Indented = true))
                w.WriteStartObject()
                w.WriteString("backend", backendName)
                w.WriteNumber("messageCount", messages.Length)
                w.WriteNumber("totalChars", totalChars)
                w.WritePropertyName("messages")
                w.WriteStartArray()
                for m in messages do
                    w.WriteStartObject()
                    w.WriteString("role", m.Role)
                    w.WriteString("content", m.Content)
                    w.WriteEndObject()
                w.WriteEndArray()
                w.WriteEndObject()
                w.Flush()
                Encoding.UTF8.GetString(buf.ToArray())
            File.WriteAllText(Path.Combine(s.Dir, "request.json"), payload)
            s.RequestLogged <- true
        | None -> ()

// ---------------------------------------------------------------------------
// Event routing — called from Config.fs onEvent
// ---------------------------------------------------------------------------

let logAgentEvent (event: AgentEvent) : unit =
    match current with
    | None -> ()
    | Some r ->
        let stepName = r.CurrentStep |> Option.map (fun s -> s.Name)
        match event with
        | RawRequest body ->
            writeRaw (rawEntry "out" "request" stepName body)
            match r.CurrentStep with
            | Some s -> File.WriteAllText(Path.Combine(s.Dir, "raw-request.txt"), body)
            | None -> ()
        | RawEvent line ->
            writeRaw (rawEntry "in" "event" stepName line)
        | Thinking t ->
            match r.CurrentStep with
            | Some s ->
                s.ThinkingWriter.Write(t)
                s.ThinkingChars <- s.ThinkingChars + t.Length
            | None -> ()
        | Text t ->
            match r.CurrentStep with
            | Some s ->
                s.TextWriter.Write(t)
                s.TextChars <- s.TextChars + t.Length
            | None -> ()
        | Metrics m ->
            // Stash, don't render yet — Metrics arrives during streaming but we want
            // it printed AFTER the THINKING and RESPONSE tails for readability.
            match r.CurrentStep with
            | Some s -> s.LastMetrics <- Some m
            | None -> ()
        | ToolUse(name, input) ->
            r.WorkflowWriter.WriteLine(sprintf "  [tool] %s: %s" name input)
            r.WorkflowWriter.Flush()
        | ToolResult output ->
            let short = if output.Length > 500 then output.[..499] + "... (truncated)" else output
            r.WorkflowWriter.WriteLine(sprintf "  [tool-result] %s" short)
            r.WorkflowWriter.Flush()
        | Error msg ->
            r.WorkflowWriter.WriteLine(sprintf "  [ERROR] %s" msg)
            r.WorkflowWriter.Flush()
        | Result _ ->
            // Final concatenated result — already captured incrementally via Text events
            // and persisted in steps/NNN/response.txt. Nothing extra to write here.
            ()

// ---------------------------------------------------------------------------
// Tail writers — flush accumulated thinking/response into workflow.log
// after askChatEx returns, so the narrative is readable top-to-bottom.
// ---------------------------------------------------------------------------

let logThinkingTail () : unit =
    match current with
    | None -> ()
    | Some r ->
        match r.CurrentStep with
        | None -> ()
        | Some s when s.ThinkingChars = 0 -> ()
        | Some s ->
            s.ThinkingWriter.Flush()
            let path = Path.Combine(s.Dir, "thinking.txt")
            let content = try File.ReadAllText(path) with _ -> ""
            r.WorkflowWriter.WriteLine("")
            r.WorkflowWriter.WriteLine(sprintf "  v THINKING (%d chars)" s.ThinkingChars)
            r.WorkflowWriter.WriteLine("")
            for line in content.Split('\n') do
                r.WorkflowWriter.WriteLine(sprintf "    %s" (trimCR line))
            r.WorkflowWriter.Flush()

let logResponseTail (response: string) : unit =
    match current with
    | None -> ()
    | Some r ->
        r.WorkflowWriter.WriteLine("")
        r.WorkflowWriter.WriteLine(sprintf "  v RESPONSE (%d chars)" response.Length)
        r.WorkflowWriter.WriteLine("")
        for line in response.Split('\n') do
            r.WorkflowWriter.WriteLine(sprintf "    %s" (trimCR line))
        r.WorkflowWriter.Flush()

let logMetricsTail () : unit =
    match current with
    | None -> ()
    | Some r ->
        match r.CurrentStep with
        | None -> ()
        | Some s ->
            match s.LastMetrics with
            | None -> ()
            | Some m ->
                // Consume: set to None so endStep's safety-net doesn't double-print.
                s.LastMetrics <- None
                let prefillS = float m.PromptEvalDurNs / 1e9
                let evalS = float m.EvalDurNs / 1e9
                let totalS = float m.TotalDurNs / 1e9
                let tokPerS = if m.EvalDurNs > 0L then float m.EvalCount / evalS else 0.0
                r.WorkflowWriter.WriteLine("")
                r.WorkflowWriter.WriteLine("  v METRICS")
                r.WorkflowWriter.WriteLine("")
                r.WorkflowWriter.WriteLine(
                    sprintf "    prompt_tokens: %d   gen_tokens: %d" m.PromptEvalCount m.EvalCount)
                r.WorkflowWriter.WriteLine(
                    sprintf "    prefill: %.2fs   gen: %.2fs @ %.1f tok/s   total: %.2fs"
                        prefillS evalS tokPerS totalS)
                r.WorkflowWriter.Flush()

// Wire the forward-declared impl so endStep's safety-net can call the real function.
logMetricsTailImpl <- logMetricsTail
