namespace FBenchmark.Store.SQLite

module Mapping =

    open BenchmarkDotNet.Reports
    open FBenchmark.Store.SQLite.Persistence
        
    open FBenchmark.Store.SQLite.Persistence
    
    let toNewBenchmarkParameters (summary: Summary) (sourceId: string) (benchmarkId: string) =
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
        
