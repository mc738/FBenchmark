namespace FBenchmark.Exporters.SQLite

open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Reports
open BenchmarkDotNet.Running
open Freql.Sqlite

type SQLiteExporter(ctx: SqliteContext) =
    
    interface IExporter with
    
        member this.ExportToFiles(summary, consoleLogger) =
            
            
                
            let r = summary.Reports |> Seq.iter (fun r -> r.BenchmarkCase.Job.Accuracy.MinIterationTime.)
            
            summary.BenchmarksCases |> Seq.map (fun i -> i)
            
            let d = r (BenchmarkCase())
            summary.Reports |> Seq.map (fun r -> r.ExecuteResults |> Seq.iter ())
            
            //summary.BenchmarksCases |> Seq.map (fun s -> )
            
            
            failwith "todo"
        
        
        member this.ExportToLog(summary, logger) = failwith "todo"
        member this.Name = failwith "todo"
    
    
    

