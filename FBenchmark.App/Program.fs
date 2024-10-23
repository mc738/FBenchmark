open System
open System.Data.SqlTypes
open System.IO
open System.Reflection
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Running
open FBenchmark.Core.Configuration
open FBenchmark.Exporters.SQLite
open FBenchmark.ExternalProcesses
open FBenchmark.Store.SQLite
open Freql.Sqlite
open FsToolbox.Core.Results

module Benchmarks =

    type QueuedBenchmark =
        { SourceId: string
          Args: string list
          Assemblies: string list }

    let run (generalSettings: FBenchGeneralSettings) (runCfg: FBenchRunConfiguration) =
        Directory.CreateDirectory runCfg.RootPath |> ignore

        use ctx =
            match File.Exists runCfg.StorePath with
            | true -> SqliteContext.Open(runCfg.StorePath)
            | false -> SqliteContext.Create(runCfg.StorePath)

        let runId = Operations.insertRun ctx runCfg.Name runCfg.Description DateTime.UtcNow

        let runRootPath = Path.Combine(runCfg.RootPath, runId)
        Directory.CreateDirectory runRootPath |> ignore

        let createAssemblyPath (sourceId: string) (assemblyName: string) =
            let fileName =
                match assemblyName.EndsWith(".dll") with
                | true -> assemblyName
                | false -> $"{assemblyName}.dll"

            Path.Combine(runCfg.RootPath, runId, "sources", sourceId, "build", fileName)

        Operations.initialize ctx

        runCfg.Benchmarks
        |> Seq.map (fun benchmark ->
            let sourceId = Operations.insertSource ctx runId benchmark.Name benchmark.Source


            let sourcesPath = Path.Combine(runCfg.RootPath, runId, "sources")
            let sourcePath = Path.Combine(runCfg.RootPath, runId, "sources", sourceId)

            let artifactsPath =
                Path.Combine(runCfg.RootPath, runId, "sources", sourceId, "artifacts")

            Directory.CreateDirectory sourcesPath |> ignore
            Directory.CreateDirectory sourcePath |> ignore
            Directory.CreateDirectory artifactsPath |> ignore

            let fetchPath = Path.Combine(runCfg.RootPath, runId, "sources", sourceId, "source")

            let buildPath =
                Path.Combine(Path.Combine(runCfg.RootPath, runId, "sources", sourceId, "build"))

            Directory.CreateDirectory fetchPath |> ignore

            // Fetch the source
            let fetchResult () =
                match benchmark.Source with
                | Local path -> failwith "todo"
                | Git source -> Git.clone generalSettings.GitPath source fetchPath
                | GitBranch(source, branch) -> Git.cloneBranch generalSettings.GitPath source branch fetchPath

            let buildResult () =
                DotNet.buildRelease
                    generalSettings.DotNetPath
                    (Path.Combine(fetchPath, benchmark.ProjectName))
                    buildPath

            fetchResult ()
            |> ActionResult.bind (fun _ -> buildResult ())
            |> ActionResult.map (fun _ ->
                { SourceId = sourceId
                  Args = benchmark.Args 
                  Assemblies = benchmark.Assemblies |> List.map (createAssemblyPath sourceId) }))
        |> Seq.iter (fun queuedPipelineResult ->

            match queuedPipelineResult with
            | ActionResult.Success queuedBenchmark ->
                let customConfig =
                    DefaultConfig.Instance
                        .WithArtifactsPath(
                            Path.Combine(runCfg.RootPath, runId, "sources", queuedBenchmark.SourceId, "artifacts")
                        )
                        .AddExporter(SQLiteExporter(ctx, runId, queuedBenchmark.SourceId))

                queuedBenchmark.Assemblies
                |> Seq.iter (fun assemblyPath ->
                    BenchmarkSwitcher
                        .FromAssembly(Assembly.LoadFile assemblyPath)
                        .Run(queuedBenchmark.Args |> Array.ofList |> Array.append [|""|], customConfig)
                    |> ignore)
            | ActionResult.Failure failureResult -> failwith "todo")

        //use store =
        //    match File.Exists

        // First fetch and build sources.
        //runCfg.s


        ()

let generalSettings = ({ GitPath = ""; DotNetPath = "" }: FBenchGeneralSettings)

let runCfg =
    ({ StorePath = ""
       RootPath = Path.Combine()
       Name = "Test"
       Description = "Test run"
       Benchmarks =
           [
               {
                   Name = failwith "todo"
                   Source = SourceType.Git ""
                   ProjectName = ""
                   Assemblies = [  ]
                   Args =  [ "-f"; "\"*\""; "--cli"; "/home/max/.dotnet/dotnet" ]
               }
           ]}
    : FBenchRunConfiguration)


let basePath =
    $"/home/max/Data/benchmarks/dotnet/example/{DateTime.UtcNow:yyyyMMddHHmmss}/"

let path = Path.Combine(basePath, "baseline_test")

Directory.CreateDirectory(path) |> ignore

let ctx = SqliteContext.Create(Path.Combine(path, "store.db"))

Operations.initialize ctx

let runId = Operations.insertRun ctx "test" "This is a test" DateTime.UtcNow

let sourceId = Operations.insertSource ctx runId "source" (SourceType.File "")

let customConfig =
    DefaultConfig.Instance
        .WithArtifactsPath(path)
        .AddExporter(SQLiteExporter(ctx, runId, sourceId))

let assemblyPath =
    "/home/max/Projects/dotnet/FBenchmark/FBenchmark.Example/bin/Release/net8.0/FBenchmark.Example.dll"

let r =
    BenchmarkSwitcher
        .FromAssembly(Assembly.LoadFile assemblyPath)
        .Run(Environment.GetCommandLineArgs(), customConfig)

// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"
