namespace FBenchmark.Store.SQLite

open System
open System.Reflection
open BenchmarkDotNet.Environments
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Portability
open BenchmarkDotNet.Reports
open BenchmarkDotNet.Running
open FBenchmark.Store.SQLite.Persistence
open Freql.Sqlite
open Perfolizer.Horology
open Perfolizer.Mathematics.OutlierDetection
open Perfolizer.Metrology

module Operations =

    let createId () = Guid.NewGuid().ToString("n")

    let insertBenchmark (ctx: SqliteContext) (sourceId: string) (summary: Summary) =

        let benchmarkId = createId ()

        ({ Id = benchmarkId
           SourceId = sourceId
           Title = summary.Title
           AllRuntimes = summary.AllRuntimes
           TotalTime = summary.TotalTime.ToString()
           IsMultipleRuntimes = summary.IsMultipleRuntimes
           LogFilePath = summary.LogFilePath
           ResultsDirectoryPath = summary.ResultsDirectoryPath
           HasCriticalValidationErrors = summary.HasCriticalValidationErrors }
        : Parameters.NewBenchmark)
        |> Operations.insertBenchmark ctx

        benchmarkId

    let insertHostInfo (ctx: SqliteContext) (benchmarkId: string) (hostEnvironmentInfo: HostEnvironmentInfo) =

        let id = createId ()

        ({ Id = id
           BenchmarkId = benchmarkId
           Architecture = hostEnvironmentInfo.Architecture
           Configuration = hostEnvironmentInfo.Configuration
           ChronometerFrequency = hostEnvironmentInfo.ChronometerFrequency.Hertz
           ChronometerResolutionNanoseconds = hostEnvironmentInfo.ChronometerResolution.Nanoseconds
           ChronometerResolutionUnitId = hostEnvironmentInfo.ChronometerResolution.Unit.FullName
           InDocker = hostEnvironmentInfo.InDocker
           JitInfo = hostEnvironmentInfo.JitInfo
           OsVersion = hostEnvironmentInfo.OsVersion.Value
           RuntimeVersion = hostEnvironmentInfo.RuntimeVersion
           HardwareIntrinsicsShort = hostEnvironmentInfo.HardwareIntrinsicsShort
           HardwareTimerKind =
             match hostEnvironmentInfo.HardwareTimerKind with
             | HardwareTimerKind.System -> "system"
             | HardwareTimerKind.Tsc -> "tsc"
             | HardwareTimerKind.Acpi -> "acpi"
             | HardwareTimerKind.Hpet -> "hpet"
             | HardwareTimerKind.Unknown -> "unknown"
             | _ -> "unknown"
           HasRyuJit = hostEnvironmentInfo.HasRyuJit
           IsMonoInstalled = hostEnvironmentInfo.IsMonoInstalled.Value
           VirtualMachineHypervisor = hostEnvironmentInfo.VirtualMachineHypervisor.Value.Name
           BenchmarkDotNetVersion = hostEnvironmentInfo.BenchmarkDotNetVersion
           DotNetSdkVersion = hostEnvironmentInfo.DotNetSdkVersion.Value
           GcAllocationQuantum = hostEnvironmentInfo.GCAllocationQuantum
           IsConcurrentGc = hostEnvironmentInfo.IsConcurrentGC
           IsServerGc = hostEnvironmentInfo.IsServerGC
           CpuMaxFrequencyHz =
             hostEnvironmentInfo.CpuInfo.Value.MaxFrequency
             |> Option.ofNullable
             |> Option.map (fun mf -> mf.Hertz)
           CpuMinFrequencyHz =
             hostEnvironmentInfo.CpuInfo.Value.MinFrequency
             |> Option.ofNullable
             |> Option.map (fun mf -> mf.Hertz)
           CpuNominalFrequencyHz =
             hostEnvironmentInfo.CpuInfo.Value.NominalFrequency
             |> Option.ofNullable
             |> Option.map (fun mf -> mf.Hertz)
           CpuProcessorName = hostEnvironmentInfo.CpuInfo.Value.ProcessorName
           CpuLogicalCoreCount =
             hostEnvironmentInfo.CpuInfo.Value.LogicalCoreCount
             |> Option.ofNullable
             |> Option.map (fun lcc -> lcc)
           CpuPhysicalCoreCount =
             hostEnvironmentInfo.CpuInfo.Value.LogicalCoreCount
             |> Option.ofNullable
             |> Option.map (fun pcc -> pcc)
           CpuPhysicalProcessorCount =
             hostEnvironmentInfo.CpuInfo.Value.LogicalCoreCount
             |> Option.ofNullable
             |> Option.map (fun ppc -> ppc) }
        : Parameters.NewBenchmarkHostInfo)
        |> Operations.insertBenchmarkHostInfo ctx

        id

    let insertHostInfoAntiVirus (ctx: SqliteContext) (hostInfoId: string) (antivirus: Antivirus) =
        let avId = createId ()

        ({ Id = avId
           HostInfoId = hostInfoId
           Name = antivirus.Name
           Path = antivirus.Path }
        : Parameters.NewBenchmarkHostInfoAntivirusInstallations)
        |> Operations.insertBenchmarkHostInfoAntivirusInstallations ctx

        avId

    let insertBenchmarkCase (ctx: SqliteContext) (benchmarkId: string) (case: BenchmarkCase) =
        let caseId = createId ()

        ({ Id = caseId
           BenchmarkId = benchmarkId
           DisplayInfo = case.DisplayInfo
           FolderInfo = case.FolderInfo
           HasArguments = case.HasArguments
           HasParameters = case.HasParameters
           ParametersCount = case.Parameters.Count
           ParametersDisplayInfo = case.Parameters.DisplayInfo
           ParametersFolderInfo = case.Parameters.FolderInfo
           ParametersPrintInfo = case.Parameters.PrintInfo
           ParametersValueInfo = case.Parameters.ValueInfo }
        : Parameters.NewBenchmarkCases)
        |> Operations.insertBenchmarkCases ctx

        caseId

    let insertMethodInfo (ctx: SqliteContext) (methodInfo: MethodInfo) =
        let id = createId ()

        ({ Id = id; Name = methodInfo.Name }: Parameters.NewMethodInfo)
        |> Operations.insertMethodInfo ctx

        id

    let insertBenchmarkCaseDescriptor (ctx: SqliteContext) (caseId: string) (descriptor: Descriptor) =
        let id = createId ()

        ({ Id = id
           CaseId = caseId
           AdditionalLogic = descriptor.AdditionalLogic
           DisplayInfo = descriptor.DisplayInfo
           FolderInfo = descriptor.FolderInfo
           MethodIndex = descriptor.MethodIndex
           GlobalCleanupMethodId =
             descriptor.GlobalCleanupMethod
             |> Option.ofObj
             |> Option.map (insertMethodInfo ctx)
           GlobalSetupMethodId =
             descriptor.GlobalSetupMethod
             |> Option.ofObj
             |> Option.map (insertMethodInfo ctx)
           IterationCleanupMethodId =
             descriptor.IterationCleanupMethod
             |> Option.ofObj
             |> Option.map (insertMethodInfo ctx)
           IterationSetupMethodId =
             descriptor.IterationSetupMethod
             |> Option.ofObj
             |> Option.map (insertMethodInfo ctx)
           OperationsPerInvoke = descriptor.OperationsPerInvoke
           WorkloadMethodDisplayInfo = descriptor.WorkloadMethodDisplayInfo }
        : Parameters.NewBenchmarkCaseDescriptors)
        |> Operations.insertBenchmarkCaseDescriptors ctx

        id

    let insertBenchmarkJob (ctx: SqliteContext) (caseId: string) (job: Job) =
        let id = createId ()
        job.Accuracy

        ({ Id = id
           CaseId = caseId
           Frozen = job.Frozen
           JobDisplayId = job.Id
           DisplayInfo = job.DisplayInfo
           FolderInfo = job.FolderInfo
           HasChanges = job.HasChanges
           ResolveId = job.ResolvedId }
        : Parameters.NewBenchmarkJobs)
        |> Operations.insertBenchmarkJobs ctx

        id

    let insertOrGetUnit (ctx: SqliteContext) (unit: MeasurementUnit) =
        match Operations.selectUnitsRecord ctx [ "WHERE id = @0" ] [ unit.FullName ] with
        | Some u -> u.Id
        | None ->
            ({ Id = unit.FullName
               Abbreviation = unit.Abbreviation
               AbbreviationAscii = unit.AbbreviationAscii
               BaseUnits = unit.BaseUnits
               FullName = unit.FullName }
            : Parameters.NewUnits)
            |> Operations.insertUnits ctx

            unit.FullName

    let insertJobAccuracy (ctx: SqliteContext) (jobId: string) (accuracy: AccuracyMode) =
        let id = createId ()

        ({ Id = id
           JobId = jobId
           Frozen = accuracy.Frozen
           EvaluateOverhead = accuracy.EvaluateOverhead
           HasChanges = accuracy.HasChanges
           OutlierMode =
             match accuracy.OutlierMode with
             | OutlierMode.DontRemove -> "dont-remove"
             | OutlierMode.RemoveUpper -> "remove-upper"
             | OutlierMode.RemoveLower -> "remove-lower"
             | OutlierMode.RemoveAll -> "remove-all"
             | _ -> "dont-remove"
           AnalyzeLaunchVariance = accuracy.AnalyzeLaunchVariance
           MaxAbsoluteErrorNanoseconds = accuracy.MaxAbsoluteError.Nanoseconds
           MaxAbsoluteErrorUnitId = insertOrGetUnit ctx accuracy.MaxAbsoluteError.Unit
           MaxRelativeError = accuracy.MaxRelativeError
           MinInvokeCount = accuracy.MinInvokeCount
           MinIterationItemNanoseconds = accuracy.MinIterationTime.Nanoseconds
           MinIterationTimeUnitId = insertOrGetUnit ctx accuracy.MinIterationTime.Unit }
        : Parameters.NewJobAccuracy)
        |> Operations.insertJobAccuracy ctx

        id

    let insertJobEnvironment (ctx: SqliteContext) (jobId: string) (environment: EnvironmentMode) =
        let id = createId ()

        ({ Id = id
           JobId = jobId
           Affinity = environment.Affinity.ToInt64()
           Frozen = environment.Frozen
           EnvironmentDisplayId = environment.Id
           Jit =
             match environment.Jit with
             | Jit.Default -> "default"
             | Jit.LegacyJit -> "legacy-jit"
             | Jit.RyuJit -> "ryu-jit"
             | Jit.Llvm -> "llvm"
             | _ -> "unknown"
           Platform =
             match environment.Platform with
             | Platform.AnyCpu -> "any-cpu"
             | Platform.X86 -> "x86"
             | Platform.X64 -> "x64"
             | Platform.Arm -> "arm"
             | Platform.Arm64 -> "arm64"
             | Platform.Wasm -> "wasm"
             | Platform.S390x -> "s390x"
             | Platform.LoongArch64 -> "loong-arch64"
             | Platform.Armv6 -> "arm-v6"
             | Platform.Ppc64le -> "ppc64le"
             | _ -> "unknown"
           HasChanges = environment.HasChanges
           LargeAddressAware = environment.LargeAddressAware
           PowerPlanMode = environment.PowerPlanMode |> Option.ofNullable |> Option.map string }
        : Parameters.NewJobEnvironments)
        |> Operations.insertJobEnvironments ctx

        id

    let saveSummary (ctx: SqliteContext) (sourceId: string) (summary: Summary) =

        let benchmarkId = insertBenchmark ctx sourceId summary

        let hostInfoId = insertHostInfo ctx benchmarkId summary.HostEnvironmentInfo

        summary.HostEnvironmentInfo.AntivirusProducts.Value
        |> Seq.iter (insertHostInfoAntiVirus ctx hostInfoId >> ignore)


        summary.Reports
        |> Seq.iter (fun report ->

            let caseId = insertBenchmarkCase ctx benchmarkId report.BenchmarkCase

            insertBenchmarkCaseDescriptor ctx caseId report.BenchmarkCase.Descriptor
            |> ignore

            let jobId = insertBenchmarkJob ctx caseId report.BenchmarkCase.Job

            insertJobAccuracy ctx jobId report.BenchmarkCase.Job.Accuracy |> ignore

            let environmentId = insertJobEnvironment ctx jobId report.BenchmarkCase.Job.Environment




            ())


        ()
