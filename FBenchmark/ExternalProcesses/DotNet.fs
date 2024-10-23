namespace FBenchmark.ExternalProcesses

open FsToolbox.ProcessWrappers

[<RequireQualifiedAccess>]
module DotNet =

    open FsToolbox.ProcessWrappers

    let buildRelease (dotnetPath: string) (source: string) (output: string) =

        let settings =
            ({ DotNet.BuildSettings.Default with
                Path = Some source
                Output = Some output
                Configuration = Some DotNet.ConfigurationType.Release })

        DotNet.build startHandler diagnosticHandler dotnetPath settings
