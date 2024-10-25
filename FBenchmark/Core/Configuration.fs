namespace FBenchmark.Core

module Configuration =

    type FBenchGeneralSettings = { GitPath: string; DotNetPath: string }

    type FBenchRunConfiguration =
        { StorePath: string
          RootPath: string
          Name: string
          Description: string
          Benchmarks: Benchmark list }

    and Benchmark =
        { Name: string
          Source: SourceType
          ProjectName: string
          Assemblies: string list
          Args: string list }

    and Source = { Name: string; Type: SourceType }

    and SourceType =
        | Local of Path: string
        | Git of Source: string
        | GitBranch of Source: string * Branch: string


        member st.GetName() =
            match st with
            | Local path -> "local"
            | Git source -> "git"
            | GitBranch(source, branch) -> "git-branch"
