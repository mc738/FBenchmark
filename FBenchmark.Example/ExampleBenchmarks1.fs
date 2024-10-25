namespace FBenchmark.Example

open BenchmarkDotNet.Attributes

type ExampleBenchmarks1() =

    [<Benchmark>]
    member _.ListTest() =

        [ 0..10000 ] |> List.fold (fun acc i -> acc @ [ $"test_{i}" ]) []
