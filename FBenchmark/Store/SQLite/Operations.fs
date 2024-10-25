namespace FBenchmark.Store.SQLite

open System
open System.Collections.Generic
open System.Reflection
open BenchmarkDotNet.Engines
open BenchmarkDotNet.Environments
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Portability
open BenchmarkDotNet.Reports
open BenchmarkDotNet.Running
open BenchmarkDotNet.Toolchains.Results
open BenchmarkDotNet.Validators
open FBenchmark.Core.Configuration
open FBenchmark.Store.SQLite.Persistence
open Freql.Sqlite
open FsToolbox.Core
open FsToolbox.Core.Strings
open FsToolbox.Extensions.Strings
open Perfolizer.Horology
open Perfolizer.Mathematics.OutlierDetection
open Perfolizer.Metrology

module Operations =

    let createId () = Guid.NewGuid().ToString("n")

    let insertRun (ctx: SqliteContext) (name: string) (description: string) (startedOn: DateTime) =
        let id = createId ()

        ({ Id = id
           Name = name
           Description = description
           StartedOn = startedOn }
        : Parameters.NewRuns)
        |> Operations.insertRuns ctx

        id

    let insertSource (ctx: SqliteContext) (runId: string) (name: string) (sourceType: SourceType) =
        let id = createId ()

        ({ Id = id
           RunId = runId
           Name = name
           SourceType = sourceType.GetName() }
        : Parameters.NewSource)
        |> Operations.insertSource ctx

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
           ChronometerResolutionUnitId = insertOrGetUnit ctx hostEnvironmentInfo.ChronometerResolution.Unit
           InDocker = hostEnvironmentInfo.InDocker
           JitInfo = hostEnvironmentInfo.JitInfo
           OsVersion = hostEnvironmentInfo.OsVersion.Value
           RuntimeVersion = hostEnvironmentInfo.RuntimeVersion
           HardwareIntrinsicsShort = hostEnvironmentInfo.HardwareIntrinsicsShort
           HardwareTimerKind =
             hostEnvironmentInfo.HardwareTimerKind
             |> Enum.GetName
             |> slugify SlugifySettings.Default

           HasRyuJit = hostEnvironmentInfo.HasRyuJit
           IsMonoInstalled = hostEnvironmentInfo.IsMonoInstalled.Value
           VirtualMachineHypervisor = "" //hostEnvironmentInfo.VirtualMachineHypervisor.Value.Name
           BenchmarkDotNetVersion = hostEnvironmentInfo.BenchmarkDotNetVersion
           DotNetSdkVersion = "" //hostEnvironmentInfo.DotNetSdkVersion.Value
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

    let insertEnvironmentGc (ctx: SqliteContext) (environmentId: string) (gcMode: GcMode) =
        let id = createId ()

        ({ Id = id
           EnvironmentId = environmentId
           Concurrent = gcMode.Concurrent
           Force = gcMode.Force
           Frozen = gcMode.Frozen
           GcDisplayId = gcMode.Id
           Server = gcMode.Server
           CpuGroups = gcMode.CpuGroups
           HasChanges = gcMode.HasChanges
           HeapCount = gcMode.HeapCount
           NoAffinitize = gcMode.NoAffinitize
           RetainVm = gcMode.RetainVm
           HeapAffinitizeMask = gcMode.HeapAffinitizeMask
           AllowVeryLargeObjects = gcMode.AllowVeryLargeObjects }
        : Parameters.NewEnvironmentGcs)
        |> Operations.insertEnvironmentGcs ctx

        id

    let insertEnvironmentRuntime (ctx: SqliteContext) (environmentId: string) (runtime: Runtime) =
        let id = createId ()

        ({ Id = id
           EnvironmentId = environmentId
           Name = runtime.Name
           RuntimeMoniker =
             runtime.RuntimeMoniker
             |> Enum.GetName
             |> Strings.slugify SlugifySettings.Default
           MsBuildMoniker = runtime.MsBuildMoniker
           IsAot = runtime.IsAOT }
        : Parameters.NewEnvironmentRuntimes)
        |> Operations.insertEnvironmentRuntimes ctx

        id

    let insertEnvironmentalVariable (ctx: SqliteContext) (environmentId: string) (variable: EnvironmentVariable) =
        let id = createId ()

        ({ Id = id
           EnvironmentalId = environmentId
           VariableKey = variable.Key
           VariableValue = variable.Value }
        : Parameters.NewEnvironmentalVariables)
        |> Operations.insertEnvironmentalVariables ctx

        id

    let insertJobMeta (ctx: SqliteContext) (jobId: string) (meta: MetaMode) =
        let id = createId ()

        ({ Id = id
           JobId = jobId
           Baseline = meta.Baseline
           Frozen = meta.Frozen
           MetaDisplayId = meta.Id
           HasChanges = meta.HasChanges
           IsDefault = meta.IsDefault
           IsMutator = meta.IsMutator }
        : Parameters.NewJobMeta)
        |> Operations.insertJobMeta ctx

        id

    let insertJobInfrastructure (ctx: SqliteContext) (jobId: string) (infrastructure: InfrastructureMode) =
        let id = createId ()

        ({ Id = id
           JobId = jobId
           ClockFrequencyHz = infrastructure.Clock |> Option.ofObj |> Option.map _.Frequency.Hertz
           ClockTitle = infrastructure.Clock |> Option.ofObj |> Option.map _.Title
           ClockIsAvailable = infrastructure.Clock |> Option.ofObj |> Option.map _.IsAvailable
           Frozen = infrastructure.Frozen
           InfrastructureDisplayId = infrastructure.Id
           ToolchainName = infrastructure.Toolchain.Name
           ToolchainIsInProcess = infrastructure.Toolchain.IsInProcess
           BuildConfiguration = infrastructure.BuildConfiguration.ToOption()
           HasChanges = infrastructure.HasChanges }
        : Parameters.NewJobInfrastructures)
        |> Operations.insertJobInfrastructures ctx

        id

    let insertInfrastructureArgument (ctx: SqliteContext) (infrastructureId: string) (argument: Argument) =
        let id = createId ()

        ({ Id = id
           InfrastructureId = infrastructureId
           TextRepresentation = argument.TextRepresentation }
        : Parameters.NewInfrastructureArguments)
        |> Operations.insertInfrastructureArguments ctx

        id

    let insertInfrastructureNugetReference (ctx: SqliteContext) (infrastructureId: string) (reference: NuGetReference) =
        let id = createId ()

        ({ Id = id
           InfrastructureId = infrastructureId
           Prerelease = reference.Prerelease
           PackageName = reference.PackageName
           PackageSource = reference.PackageSource.ToString()
           PackageVersion = reference.PackageVersion }
        : Parameters.NewInfrastructureNugetReferences)
        |> Operations.insertInfrastructureNugetReferences ctx

        id

    let insertJobRun (ctx: SqliteContext) (jobId: string) (runMode: RunMode) =
        let id = createId ()

        ({ Id = id
           JobId = jobId
           Frozen = runMode.Frozen
           RunDisplayId = runMode.Id
           HasChanges = runMode.HasChanges
           InvocationCount = runMode.InvocationCount
           IterationCount = runMode.IterationCount
           IterationTimeNanoseconds = runMode.IterationTime.Nanoseconds
           IterationTimeUnitId = insertOrGetUnit ctx runMode.IterationTime.Unit
           LaunchCount = runMode.LaunchCount
           MemoryRandomization = runMode.MemoryRandomization
           RunStrategy =
             match runMode.RunStrategy with
             | RunStrategy.Throughput -> "throughput"
             | RunStrategy.ColdStart -> "cold-start"
             | RunStrategy.Monitoring -> "monitoring"
             | _ -> "unknown"
           UnrollFactor = runMode.UnrollFactor
           WarmupCount = runMode.WarmupCount
           MaxIterationCount = runMode.MaxIterationCount
           MinIterationCount = runMode.MinIterationCount
           MaxWarmupIterationCount = runMode.MaxWarmupIterationCount
           MinWarmupIterationCount = runMode.MinWarmupIterationCount }
        : Parameters.NewJobRuns)
        |> Operations.insertJobRuns ctx

        id

    let insertBenchmarkReport (ctx: SqliteContext) (caseId: string) (report: BenchmarkReport) =
        let id = createId ()

        ({ Id = id
           CaseId = caseId
           Success = report.Success
           GcStatsGen0Collections = report.GcStats.Gen0Collections
           GcStatsGen1Collections = report.GcStats.Gen1Collections
           GcStatsGen2Collections = report.GcStats.Gen2Collections
           GcStatsTotalOperations = report.GcStats.TotalOperations
           GcStatsTotalAllocatedBytes = report.GcStats.GetTotalAllocatedBytes(false) |> Option.ofNullable
           GcStatsBytesAllocatedPerOperation =
             report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase)
             |> Option.ofNullable
           ResultsStatsKurtosis = report.ResultStatistics.Kurtosis
           ResultsStatsMax = report.ResultStatistics.Max
           ResultsStatsMean = report.ResultStatistics.Mean
           ResultsStatsMedian = report.ResultStatistics.Median
           ResultsStatsN = report.ResultStatistics.N
           ResultsStatsQ1 = report.ResultStatistics.Q1
           ResultsStatsQ3 = report.ResultStatistics.Q3
           ResultsStatsSkewness = report.ResultStatistics.Skewness
           ResultsStatsVariance = report.ResultStatistics.Variance
           ResultsStatsInterquartileRange = report.ResultStatistics.InterquartileRange
           ResultsStatsLowerFence = report.ResultStatistics.LowerFence
           ResultsStatsStandardDeviation = report.ResultStatistics.StandardDeviation
           ResultsStatsStandardError = report.ResultStatistics.StandardError
           ResultsStatsUpperFence = report.ResultStatistics.UpperFence
           ResultsStatsConfidenceIntervalLevel =
             report.ResultStatistics.ConfidenceInterval.Level
             |> Enum.GetName
             |> Strings.slugify SlugifySettings.Default
           ResultsStatsConfidenceIntervalLower = report.ResultStatistics.ConfidenceInterval.Lower
           ResultsStatsConfidenceIntervalMargin = report.ResultStatistics.ConfidenceInterval.Margin
           ResultsStatsConfidenceIntervalMean = report.ResultStatistics.ConfidenceInterval.Mean
           ResultsStatsConfidenceIntervalN = report.ResultStatistics.ConfidenceInterval.N
           ResultsStatsConfidenceIntervalUpper = report.ResultStatistics.ConfidenceInterval.Upper
           ResultsStatsConfidenceIntervalStandardError = report.ResultStatistics.ConfidenceInterval.StandardError
           ResultsStatsPercentilesP0 = report.ResultStatistics.Percentiles.P0
           ResultsStatsPercentilesP25 = report.ResultStatistics.Percentiles.P25
           ResultsStatsPercentilesP50 = report.ResultStatistics.Percentiles.P50
           ResultsStatsPercentilesP67 = report.ResultStatistics.Percentiles.P67
           ResultsStatsPercentilesP80 = report.ResultStatistics.Percentiles.P80
           ResultsStatsPercentilesP85 = report.ResultStatistics.Percentiles.P85
           ResultsStatsPercentilesP90 = report.ResultStatistics.Percentiles.P90
           ResultsStatsPercentilesP95 = report.ResultStatistics.Percentiles.P95
           ResultsStatsPercentilesP100 = report.ResultStatistics.Percentiles.P100 }
        : Parameters.NewBenchmarkReports)
        |> Operations.insertBenchmarkReports ctx

        id

    let insertReportMetric (ctx: SqliteContext) (reportId: string) (metric: KeyValuePair<string, Metric>) =
        let id = createId ()

        ({ Id = id
           ReportId = failwith "todo"
           MetricKey = failwith "todo"
           MetricValue = failwith "todo"
           MetricId = failwith "todo"
           Legend = failwith "todo"
           Unit = failwith "todo"
           DisplayName = failwith "todo"
           NumberFormat = failwith "todo"
           UnitType = failwith "todo"
           PriorityInCategory = failwith "todo"
           TheGreaterTheBetter = failwith "todo" }
        : Parameters.NewReportMetrics)
        |> Operations.insertReportMetrics ctx

        id

    let insertReportMeasurement (ctx: SqliteContext) (reportId: string) (measurement: Measurement) =
        let id = createId ()

        ({ Id = id
           ReportId = reportId
           Nanoseconds = measurement.Nanoseconds
           Operations = measurement.Operations
           IterationIndex = measurement.IterationIndex
           IterationMode = measurement.IterationMode |> Enum.GetName |> slugify SlugifySettings.Default
           IterationStage = measurement.IterationStage |> Enum.GetName |> slugify SlugifySettings.Default
           LaunchIndex = measurement.LaunchIndex }
        : Parameters.NewReportMeasurements)
        |> Operations.insertReportMeasurements ctx

        id

    let insertReportOriginalValue
        (ctx: SqliteContext)
        (reportId: string)
        (value: float)
        (isUpperOutlier: bool)
        (isLowerOutlier: bool)
        =
        let id = createId ()

        ({ Id = id
           ReportId = reportId
           ResultValue = value
           IsUpperOutlier = isUpperOutlier
           IsLowerOutlier = isLowerOutlier }
        : Parameters.NewReportOriginalValues)
        |> Operations.insertReportOriginalValues ctx

        id

    let insertBuildResult (ctx: SqliteContext) (reportId: string) (buildResult: BuildResult) =
        let id = createId ()

        ({ Id = id
           ReportId = reportId
           ErrorMessage = buildResult.ErrorMessage.ToOption()
           IsBuildSuccess = buildResult.IsBuildSuccess
           IsGenerateSuccess = buildResult.IsGenerateSuccess
           ExecutablePath = buildResult.ArtifactsPaths.ExecutablePath
           ProgramName = buildResult.ArtifactsPaths.ProgramName
           AppConfigPath = buildResult.ArtifactsPaths.AppConfigPath.ToOption()
           BinariesDirectoryPath = buildResult.ArtifactsPaths.BinariesDirectoryPath.ToOption()
           IntermediateDirectoryPath = buildResult.ArtifactsPaths.IntermediateDirectoryPath.ToOption()
           PackageDirectoryName = buildResult.ArtifactsPaths.PackagesDirectoryName.ToOption()
           ProgramCodePath = buildResult.ArtifactsPaths.ProgramCodePath.ToOption()
           ProjectFilePath = buildResult.ArtifactsPaths.ProjectFilePath.ToOption()
           BuildArtifactsDirectoryPath = buildResult.ArtifactsPaths.BuildArtifactsDirectoryPath.ToOption()
           BuildScriptFilePath = buildResult.ArtifactsPaths.BuildScriptFilePath.ToOption()
           NugetConfigPath = buildResult.ArtifactsPaths.NuGetConfigPath.ToOption()
           RootArtifactsFolderPath = buildResult.ArtifactsPaths.RootArtifactsFolderPath.ToOption() }
        : Parameters.NewBuildResults)
        |> Operations.insertBuildResults ctx

        id

    let insertBuildArtifact (ctx: SqliteContext) (buildResultId: string) (path: string) =
        let id = createId ()

        ({ Id = id
           BuildResultId = buildResultId
           Path = path }
        : Parameters.NewBuildArtifacts)
        |> Operations.insertBuildArtifacts ctx

        id

    let insertExecuteResult (ctx: SqliteContext) (reportId: string) (executeResult: ExecuteResult) =
        let id = createId ()

        ({ Id = id
           ReportId = reportId
           ExitCode = executeResult.ExitCode |> Option.ofNullable |> Option.map int64
           IsSuccess = executeResult.IsSuccess
           ProcessId = executeResult.ProcessId |> Option.ofNullable |> Option.map int64 }
        : Parameters.NewExecutionResults)
        |> Operations.insertExecutionResults ctx

        id

    let insertExecuteResultError (ctx: SqliteContext) (executeResultId: string) (error: string) =
        let id = createId ()

        ({ Id = id
           ExecutionResultId = executeResultId
           Error = error }
        : Parameters.NewExecutionResultErrors)
        |> Operations.insertExecutionResultErrors ctx

        id

    let insertExecutionResultMeasurement (ctx: SqliteContext) (executionResultId: string) (measurement: Measurement) =
        let id = createId ()

        ({ Id = id
           ExecutionResultId = executionResultId
           Nanoseconds = measurement.Nanoseconds
           Operations = measurement.Operations
           IterationIndex = measurement.IterationIndex
           IterationMode = measurement.IterationMode |> Enum.GetName |> slugify SlugifySettings.Default
           IterationStage = measurement.IterationStage |> Enum.GetName |> slugify SlugifySettings.Default
           LaunchIndex = measurement.LaunchIndex }
        : Parameters.NewExecutionResultMeasurements)
        |> Operations.insertExecutionResultMeasurements ctx

        id

    let insertExecutionResultItem (ctx: SqliteContext) (executionResultId: string) (value: string) =
        let id = createId ()

        ({ Id = id
           ExecutionResultId = executionResultId
           Value = value }
        : Parameters.NewExecutionResultItems)
        |> Operations.insertExecutionResultItems ctx

        id

    let insertExecutionResultPrefixedLine (ctx: SqliteContext) (executionResultId: string) (prefixedLine: string) =
        let id = createId ()

        ({ Id = id
           ExecutionResultId = executionResultId
           PrefixedLine = prefixedLine }
        : Parameters.NewExecutionResultPrefixedLines)
        |> Operations.insertExecutionResultPrefixedLines ctx

        id

    let insertExecutionResultStandardOutputLine (ctx: SqliteContext) (executionResultId: string) (line: string) =
        let id = createId ()

        ({ Id = id
           ExecutionResultId = executionResultId
           Line = line }
        : Parameters.NewExecutionResultStandardOutputLines)
        |> Operations.insertExecutionResultStandardOutputLines ctx

        id

    let insertGenerateResult (ctx: SqliteContext) (reportId: string) (result: GenerateResult) =
        let id = createId ()

        ({ Id = id
           ReportId = reportId
           IsGenerateSuccess = result.IsGenerateSuccess
           ExecutablePath = result.ArtifactsPaths.ExecutablePath
           ProgramName = result.ArtifactsPaths.ProgramName
           AppConfigPath = result.ArtifactsPaths.AppConfigPath.ToOption()
           BinariesDirectoryPath = result.ArtifactsPaths.BinariesDirectoryPath.ToOption()
           IntermediateDirectoryPath = result.ArtifactsPaths.IntermediateDirectoryPath.ToOption()
           PackagesDirectoryName = result.ArtifactsPaths.PackagesDirectoryName.ToOption()
           ProgramCodePath = result.ArtifactsPaths.ProgramCodePath.ToOption()
           ProjectFilePath = result.ArtifactsPaths.ProjectFilePath.ToOption()
           BuildArtifactsDirectoryPath = result.ArtifactsPaths.BuildArtifactsDirectoryPath.ToOption()
           BuildScriptFilePath = result.ArtifactsPaths.BuildScriptFilePath.ToOption()
           NugetConfigPath = result.ArtifactsPaths.NuGetConfigPath.ToOption()
           RootArtifactsFolderPath = result.ArtifactsPaths.RootArtifactsFolderPath.ToOption() }
        : Parameters.NewGenerateResults)
        |> Operations.insertGenerateResults ctx

        id

    let insertGenerateArtifact (ctx: SqliteContext) (generateResultId: string) (path: string) =
        let id = createId ()

        ({ Id = id
           GenerateResultId = generateResultId
           Path = path }
        : Parameters.NewGenerateArtifacts)
        |> Operations.insertGenerateArtifacts ctx

        id

    let insertValidationError (ctx: SqliteContext) (caseId: string) (validationError: ValidationError) =
        let id = createId ()

        ({ Id = id
           CaseId = caseId
           Message = validationError.Message
           IsCritical = validationError.IsCritical }
        : Parameters.NewValidationError)
        |> Operations.insertValidationError ctx

        id

    let initialize (ctx: SqliteContext) = Initialization.run true ctx

    let saveSummary (ctx: SqliteContext) (sourceId: string) (summary: Summary) =

        let benchmarkId = insertBenchmark ctx sourceId summary

        let hostInfoId = insertHostInfo ctx benchmarkId summary.HostEnvironmentInfo

        summary.HostEnvironmentInfo.AntivirusProducts.Value
        |> Seq.iter (insertHostInfoAntiVirus ctx hostInfoId >> ignore)


        //printfn $"************ Reports: {summary.Reports.Length}"

        summary.Reports
        |> Seq.iter (fun report ->

            let caseId = insertBenchmarkCase ctx benchmarkId report.BenchmarkCase

            summary.ValidationErrors
            |> Seq.filter (fun ve -> ve.BenchmarkCase = report.BenchmarkCase)
            |> Seq.iter (insertValidationError ctx caseId >> ignore)

            insertBenchmarkCaseDescriptor ctx caseId report.BenchmarkCase.Descriptor
            |> ignore

            let jobId = insertBenchmarkJob ctx caseId report.BenchmarkCase.Job

            insertJobAccuracy ctx jobId report.BenchmarkCase.Job.Accuracy |> ignore

            let environmentId =
                insertJobEnvironment ctx jobId report.BenchmarkCase.Job.Environment

            insertEnvironmentGc ctx environmentId report.BenchmarkCase.Job.Environment.Gc
            |> ignore

            report.BenchmarkCase.Job.Environment.Runtime
            |> Option.ofObj
            |> Option.iter (insertEnvironmentRuntime ctx environmentId >> ignore)

            if report.BenchmarkCase.Job.Environment.EnvironmentVariables <> null then
                report.BenchmarkCase.Job.Environment.EnvironmentVariables
                |> Seq.iter (insertEnvironmentalVariable ctx environmentId >> ignore)

            let infrastructureId =
                insertJobInfrastructure ctx jobId report.BenchmarkCase.Job.Infrastructure

            if report.BenchmarkCase.Job.Infrastructure.Arguments <> null then
                report.BenchmarkCase.Job.Infrastructure.Arguments
                |> Seq.iter (insertInfrastructureArgument ctx infrastructureId >> ignore)

            if report.BenchmarkCase.Job.Infrastructure.NuGetReferences <> null then
                report.BenchmarkCase.Job.Infrastructure.NuGetReferences
                |> Seq.iter (insertInfrastructureNugetReference ctx infrastructureId >> ignore)

            insertJobMeta ctx jobId report.BenchmarkCase.Job.Meta |> ignore

            insertJobRun ctx jobId report.BenchmarkCase.Job.Run |> ignore


            //report.Metrics |> Seq.iter (fun i -> i)

            //report.BenchmarkCase.Job.Infrastructure.
            let reportId = insertBenchmarkReport ctx caseId report


            report.ResultStatistics.OriginalValues
            |> Seq.iter (fun ov ->
                insertReportOriginalValue
                    ctx
                    reportId
                    ov
                    (report.ResultStatistics.IsUpperOutlier(ov))
                    (report.ResultStatistics.IsLowerOutlier(ov))
                |> ignore)

            report.Metrics |> Seq.iter (insertReportMetric ctx reportId >> ignore)

            report.AllMeasurements
            |> Seq.iter (insertReportMeasurement ctx reportId >> ignore)

            let buildReportId = insertBuildResult ctx reportId report.BuildResult

            report.BuildResult.ArtifactsToCleanup
            |> Seq.iter (insertBuildArtifact ctx buildReportId >> ignore)

            report.ExecuteResults
            |> Seq.iter (fun er ->
                let executeId = insertExecuteResult ctx reportId er

                er.Errors |> Seq.iter (insertExecuteResultError ctx executeId >> ignore)

                er.Measurements
                |> Seq.iter (insertExecutionResultMeasurement ctx executeId >> ignore)

                er.Results |> Seq.iter (insertExecutionResultItem ctx executeId >> ignore)

                er.PrefixedLines
                |> Seq.iter (insertExecutionResultPrefixedLine ctx executeId >> ignore)

                er.StandardOutput
                |> Seq.iter (insertExecutionResultStandardOutputLine ctx executeId >> ignore))

            let generateResultId = insertGenerateResult ctx reportId report.GenerateResult

            report.GenerateResult.ArtifactsToCleanup
            |> Seq.iter (insertGenerateArtifact ctx generateResultId >> ignore))
