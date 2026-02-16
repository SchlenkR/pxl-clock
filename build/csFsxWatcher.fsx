open System
open System.IO
open System.Diagnostics
open System.Threading

// Load .env from the repo root (cwd)
let envFile = Path.Combine(Environment.CurrentDirectory, ".env")
let defaultEnvContent = """
# Comma-separated list of device names and addresses (IP, network name) to send frames to.
# Format: name:address (e.g. myDevice:192.168.1.42)
PXL_DEV_DEVICES=

# Name of the device (from PXL_DEV_DEVICES) to send frames to. Leave empty to not send to any device.
PXL_SEND_TO_DEV_DEVICE=

# Set to false to disable the simulator.
PXL_SEND_TO_SIMULATOR=true
"""

let loadEnvFile () =
    if not (File.Exists(envFile)) then
        File.WriteAllText(envFile, defaultEnvContent)
        printfn $"Created default .env at {envFile}"
    printfn $"Loading .env from {envFile}:"
    for line in File.ReadAllLines(envFile) do
        let trimmed = line.Trim()
        if trimmed <> "" && not (trimmed.StartsWith("#")) then
            match trimmed.Split('=', 2) with
            | [| key; value |] ->
                Environment.SetEnvironmentVariable(key.Trim(), value.Trim())
                printfn $"  {key.Trim()} = {value.Trim()}"
            | _ -> ()

loadEnvFile()

let watchPath =
    match Environment.GetEnvironmentVariable("PXL_WATCH_PATH") with
    | null | "" -> Path.Combine(__SOURCE_DIRECTORY__, "..", "apps")
    | path -> Path.GetFullPath(path)

let mutable currentProcess: Process option = None
let processLock = obj()

let startDotnetRunForFile (filePath: string) =
    lock processLock (fun () ->
        // Kill existing process if running
        match currentProcess with
        | Some proc when not proc.HasExited ->
            printfn "Killing existing process..."
            try
                proc.Kill(entireProcessTree = true)
                proc.WaitForExit(2000) |> ignore
            with ex ->
                printfn $"Warning: Error killing process: {ex.Message}"
        | _ -> ()

        // Determine command based on file extension
        let isFsx = filePath.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase)
        let command, args = 
            if isFsx then "dotnet", $"fsi \"{filePath}\""
            else "dotnet", $"run \"{filePath}\""

        printfn $"Starting {command} {args}"
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- command
        startInfo.Arguments <- args
        startInfo.WorkingDirectory <- watchPath
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true

        let proc = new Process()
        proc.StartInfo <- startInfo
        
        proc.OutputDataReceived.Add(fun args ->
            if not (isNull args.Data) then
                printfn $"[dotnet] {args.Data}")
        
        proc.ErrorDataReceived.Add(fun args ->
            if not (isNull args.Data) then
                eprintfn $"[dotnet ERROR] {args.Data}")

        proc.Start() |> ignore
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()
        
        currentProcess <- Some proc
        printfn "Process started."
    )

let mutable lastChangeTime = DateTime.MinValue
let mutable lastChangedFile = ""
let debounceMs = 500.0

let mutable lastRunFile = ""

let onCodeChanged (e: FileSystemEventArgs) =
    let now = DateTime.Now
    if (now - lastChangeTime).TotalMilliseconds < debounceMs && lastChangedFile = e.FullPath then
        ()
    else
        lastChangeTime <- now
        lastChangedFile <- e.FullPath
        lastRunFile <- e.FullPath

        let green = "\u001b[32m"
        let reset = "\u001b[0m"

        printfn $"{green}File changed: {e.FullPath}{reset}"
        printfn $"{green}Restarting...{reset}"

        startDotnetRunForFile e.FullPath

let onEnvChanged (e: FileSystemEventArgs) =
    let now = DateTime.Now
    if (now - lastChangeTime).TotalMilliseconds < debounceMs && lastChangedFile = e.FullPath then
        ()
    else
        lastChangeTime <- now
        lastChangedFile <- e.FullPath

        let yellow = "\u001b[33m"
        let reset = "\u001b[0m"

        printfn $"{yellow}.env changed — reloading environment variables...{reset}"
        loadEnvFile()

        if lastRunFile <> "" then
            printfn $"{yellow}Restarting last run: {lastRunFile}{reset}"
            startDotnetRunForFile lastRunFile

do
    for pattern in ["*.cs"; "*.fsx"] do
        let watcher = new FileSystemWatcher(watchPath, pattern)
        watcher.IncludeSubdirectories <- true
        watcher.NotifyFilter <- NotifyFilters.FileName ||| NotifyFilters.LastWrite
        watcher.Changed.Add(onCodeChanged)
        watcher.Created.Add(onCodeChanged)
        watcher.Renamed.Add(onCodeChanged)
        watcher.EnableRaisingEvents <- true

    if File.Exists(envFile) then
        let envDir = Path.GetDirectoryName(envFile)
        let envName = Path.GetFileName(envFile)
        let envWatcher = new FileSystemWatcher(envDir, envName)
        envWatcher.NotifyFilter <- NotifyFilters.LastWrite
        envWatcher.Changed.Add(onEnvChanged)
        envWatcher.EnableRaisingEvents <- true

printfn $"Watching {watchPath} for C# and F# file changes (Ctrl+C to exit)..."
printfn "Waiting for file changes... (No initial run - modify a .cs or .fsx file to start)"

// Wait for Ctrl+C
let exitEvent = new ManualResetEvent(false)
Console.CancelKeyPress.Add(fun args ->
    printfn "Ctrl+C pressed. Shutting down..."
    args.Cancel <- true
    
    // Kill the dotnet process
    lock processLock (fun () ->
        match currentProcess with
        | Some proc when not proc.HasExited ->
            try
                proc.Kill(entireProcessTree = true)
                proc.WaitForExit(2000) |> ignore
            with _ -> ()
        | _ -> ()
    )
    
    exitEvent.Set() |> ignore)

exitEvent.WaitOne() |> ignore

printfn "File watcher closed."
