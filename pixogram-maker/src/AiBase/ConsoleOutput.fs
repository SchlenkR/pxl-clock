module AiBase.ConsoleOutput

open System
open System.IO
open AiBase.Agent

let private boxWidth = 90
let private indent0 = 0
let private indent1 = 10

let colored (color: ConsoleColor) (text: string) =
    let prev = Console.ForegroundColor
    Console.ForegroundColor <- color
    Console.Write(text)
    Console.ForegroundColor <- prev

let coloredLn (color: ConsoleColor) (text: string) =
    colored color text
    Console.WriteLine()

let newLine () = Console.WriteLine()

let private wrapLine (width: int) (line: string) =
    if line.Length = 0 then [ "" ]
    else
        let words = line.Split(' ')
        let lines = ResizeArray<string>()
        let current = Text.StringBuilder()

        for word in words do
            if current.Length > 0 && current.Length + 1 + word.Length > width then
                lines.Add(current.ToString())
                current.Clear() |> ignore
            if current.Length > 0 then
                current.Append(' ') |> ignore
            current.Append(word) |> ignore

        if current.Length > 0 then
            lines.Add(current.ToString())

        if lines.Count = 0 then [ "" ] else Seq.toList lines

let private wordWrap (width: int) (text: string) =
    text.Split('\n')
    |> Array.toList
    |> List.collect (wrapLine width)

let private printBox (indent: int) (color: ConsoleColor) (header: string) (text: string) =
    let pad = String(' ', indent)
    let innerWidth = boxWidth - 2
    let top = $"{pad}╭{String('─', innerWidth)}╮"
    let bottom = $"{pad}╰{String('─', innerWidth)}╯"
    let separator = $"{pad}├{String('─', innerWidth)}┤"

    coloredLn color top
    let headerContent = $" {header}".PadRight(innerWidth)
    coloredLn color $"{pad}│{headerContent}│"
    coloredLn color separator

    let wrapped = wordWrap (innerWidth - 2) (text.Trim())

    for line in wrapped do
        let content = $" {line}".PadRight(innerWidth)
        colored color $"{pad}│"
        Console.Write(content)
        coloredLn color "│"

    coloredLn color bottom

let printAgentResponse (index: int) (name: string) (color: ConsoleColor) (text: string) =
    let indent = if index = 0 then indent0 else indent1
    printBox indent color name text

let printEvent (index: int) (name: string) (color: ConsoleColor) =
    let pad = String(' ', if index = 0 then indent0 else indent1)
    let mutable wasStreaming = false

    let endStreamIfNeeded () =
        if wasStreaming then
            Console.WriteLine()
            wasStreaming <- false

    fun (event: AgentEvent) ->
        match event with
        | Thinking t ->
            endStreamIfNeeded()
            colored ConsoleColor.DarkGray $"{pad}  [{name} thinking] "
            coloredLn ConsoleColor.DarkGray t
        | Text t ->
            if not wasStreaming then
                colored color $"{pad}  [{name}] "
                wasStreaming <- true
            colored color t
        | ToolUse(tool, input) ->
            endStreamIfNeeded()
            coloredLn ConsoleColor.Magenta $"{pad}  [{name} -> {tool}] {input}"
        | ToolResult output ->
            endStreamIfNeeded()
            let short = if output.Length > 200 then output.[..199] + "..." else output
            coloredLn ConsoleColor.Magenta $"{pad}  [{name} <- result] {short}"
        | Result _ ->
            endStreamIfNeeded()
        | Error e ->
            endStreamIfNeeded()
            coloredLn ConsoleColor.Red $"{pad}  [{name} ERROR] {e}"
        | Metrics _ -> ()
        // Raw events are noisy by design — they go to raw.jsonl on disk, not the console.
        | RawRequest _ | RawEvent _ -> ()

let private formatEvent (name: string) (event: AgentEvent) =
    match event with
    | Thinking t -> $"[{name} thinking] {t}"
    | Text t -> $"[{name}] {t}"
    | ToolUse(tool, input) -> $"[{name} -> {tool}] {input}"
    | ToolResult output -> $"[{name} <- result] {output}"
    | Result t -> $"[{name} result] {t}"
    | Error e -> $"[{name} ERROR] {e}"
    | Metrics m -> $"[{name} metrics] prompt={m.PromptEvalCount} gen={m.EvalCount}"
    | RawRequest _ -> $"[{name} raw-request]"
    | RawEvent _ -> $"[{name} raw-event]"

let logEvent (logPath: string) (index: int) (name: string) (color: ConsoleColor) =
    let consoleFn = printEvent index name color
    let logLock = obj()

    let timestamp () = DateTime.Now.ToString("HH:mm:ss.fff")

    fun (event: AgentEvent) ->
        consoleFn event
        let ts = timestamp()
        let line = $"[{ts}] {formatEvent name event}"
        lock logLock (fun () -> File.AppendAllText(logPath, line + "\n"))

let logLine (logPath: string) (text: string) =
    let ts = DateTime.Now.ToString("HH:mm:ss.fff")
    File.AppendAllText(logPath, $"[{ts}] {text}\n")
