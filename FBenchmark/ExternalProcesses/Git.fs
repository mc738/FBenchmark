namespace FBenchmark.ExternalProcesses

open System.IO

module Git =
    
    open FsToolbox.ProcessWrappers
    
    let clone (gitPath: string) (repositoryUrl: string) (outputDirectory: string) =
        
        let settings = ({
            Git.CloneSettings.Default with
                Repository = repositoryUrl
                Directory = Some outputDirectory
        })
        
        
        Git.clone startHandler diagnosticHandler gitPath settings 

    let cloneBranch (gitPath: string) (repositoryUrl: string) (branch: string) (outputDirectory: string) =
        
        let settings = ({
            Git.CloneSettings.Default with
                Repository = repositoryUrl
                Directory = Some outputDirectory
                Branch = Some branch 
        })
        
        
        Git.clone startHandler diagnosticHandler gitPath settings 

