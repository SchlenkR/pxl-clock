module AiBase.DotEnv

open System
open System.IO

let load () =
    let mutable dir = Directory.GetCurrentDirectory()
    let mutable found = false
    while not found && not (isNull dir) do
        let path = Path.Combine(dir, ".env")
        if File.Exists(path) then
            for line in File.ReadAllLines(path) do
                let trimmed = line.Trim()
                if trimmed <> "" && not (trimmed.StartsWith("#")) then
                    match trimmed.IndexOf('=') with
                    | -1 -> ()
                    | i ->
                        let key = trimmed.Substring(0, i).Trim()
                        let raw = trimmed.Substring(i + 1).Trim()
                        let value =
                            if raw.Length >= 2 && raw.[0] = '"' && raw.[raw.Length - 1] = '"' then
                                raw.Substring(1, raw.Length - 2)
                            elif raw.Length >= 2 && raw.[0] = ''' && raw.[raw.Length - 1] = ''' then
                                raw.Substring(1, raw.Length - 2)
                            else raw
                        if isNull (Environment.GetEnvironmentVariable(key)) then
                            Environment.SetEnvironmentVariable(key, value)
            found <- true
        else
            dir <- Path.GetDirectoryName(dir)
