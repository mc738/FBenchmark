namespace FBenchmark.Exporters.SQLite

open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Reports
open BenchmarkDotNet.Running
open FBenchmark.Store.SQLite
open Freql.Sqlite

type SQLiteExporter(ctx: SqliteContext, runId: string, sourceId: string) =
    
    interface IExporter with
    
        member this.ExportToFiles(summary, consoleLogger) =
            
            Operations.saveSummary ctx sourceId summary
            
            
            
                
            //let r = summary.ValidationErrors |> Seq.iter (fun ve -> ve.)
            
            //summary.BenchmarksCases |> Seq.map (fun i -> i.)
            
            //let d = r (BenchmarkCase())
            //summary.Reports |> Seq.map (fun r -> r.ExecuteResults |> Seq.iter ())
            
            //summary.BenchmarksCases |> Seq.map (fun s -> )
            
            
            []
        
        
        member this.ExportToLog(summary, logger) = failwith "todo"
        member this.Name = "sqlite-exporter"
    
    
    

