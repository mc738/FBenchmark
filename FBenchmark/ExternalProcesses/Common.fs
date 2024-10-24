namespace FBenchmark.ExternalProcesses

open System
open System.Diagnostics
open CommandLine
open FsToolbox.Core
open FsToolbox.Core.ConsoleIO

[<AutoOpen>]
module Common =

    let startHandler =
        ({ Timeout = 1000 * 60 * 15 |> Some
           StartDirectory = None
           StartInfoBuilder =
             fun psi ->
                 psi.WindowStyle <- ProcessWindowStyle.Hidden
                 psi.UseShellExecute <- false
                 psi.CreateNoWindow <- true
                 psi }
        : Processes.ProcessStartHandler)

    let diagnosticHandler =
        ({ Logger = Some <| fun message -> cprintfn ConsoleColor.DarkGray $"{message}"
           StandardOutputHandler = Some <| fun message -> printfn $"{message}"
           StandardErrorHandler = Some <| fun message -> printError $"{message}" }
        : Processes.ProcessDiagnosticHandler)



    ()
