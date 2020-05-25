#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"

open System.IO

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators


// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "ExcelProvider"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "This library is for the .NET platform implementing a Excel type provider."

// Read additional information from the release notes document
let release = ReleaseNotes.load "RELEASE_NOTES.md"

// Helper active pattern for project types
let (|Fsproj|) (projFileName : string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | _ -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)



Target.create "Clean" (fun _ ->
    Trace.log "--Cleaning 'bin' and 'temp' directories"
    Shell.cleanDirs [ "bin"; "temp" ]

    )

Target.create "AssemblyInfo" (fun _ ->
    Trace.log "--Creating new assembly files with appropriate version number and info"
    let getAssemblyInfoAttributes projectName =
        [ AssemblyInfo.Title(projectName)
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
        let projectName = Path.GetFileNameWithoutExtension(projectPath)
        let directoryName = Path.GetDirectoryName(projectPath)
        let assemblyInfoAttributes = getAssemblyInfoAttributes projectName
        (projectPath, projectName, directoryName, assemblyInfoAttributes)

    !!"src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, _, folderName, attributes) ->
        match projFileName with
        | Fsproj ->
            let fileName = folderName + @"/" + "AssemblyInfo.fs"
            AssemblyInfoFile.createFSharp fileName attributes))

Target.create "All" ignore

"Clean" ==> "AssemblyInfo" ==> "All"

Target.runOrDefault "All"
