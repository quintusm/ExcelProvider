#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"

open System.IO

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators


Target.initEnvironment ()

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "ExcelProvider"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary =
    "This library is for the .NET platform implementing a Excel type provider."

// Read additional information from the release notes document
let release = ReleaseNotes.load "RELEASE_NOTES.md"

// Helper active pattern for project types
let (|Fsproj|) (projFileName: string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | _ -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)


// If you want to use MSBuild instead of dotnet build define an environment variable called "USE_MSBUILD"
let useMsBuildToolchain =
    not (isNull (Environment.environVar "USE_MSBUILD"))

Trace.log "--Installing DotNet Core SDK if necessary"

let install =
    lazy
        (DotNet.install (fun opt ->
            { opt with
                  Version = DotNet.Version "2.1.806" }))

let getSdkPath () = install.Value

Trace.log (sprintf "Value of getSdkPath = %A" getSdkPath)



Target.create "Clean" (fun _ ->
    Trace.log "--Cleaning various directories"
    !! "bin"
    ++ "temp"
    ++ "test/bin"
    ++ "test/obj"
    ++ "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs)

Target.create "AssemblyInfo" (fun _ ->
    Trace.log "--Creating new assembly files with appropriate version number and info"

    let getAssemblyInfoAttributes projectName =
        [ AssemblyInfo.Title(projectName)
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
        let projectName =
            Path.GetFileNameWithoutExtension(projectPath)

        let directoryName = Path.GetDirectoryName(projectPath)
        let assemblyInfoAttributes = getAssemblyInfoAttributes projectName
        (projectPath, projectName, directoryName, assemblyInfoAttributes)

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, _, folderName, attributes) ->
        match projFileName with
        | Fsproj ->
            let fileName = folderName + @"/" + "AssemblyInfo.fs"
            AssemblyInfoFile.createFSharp fileName attributes))

Target.create "Build" (fun _ ->
    Trace.log "--Building the binary files for distribution"
    if useMsBuildToolchain then
        Trace.log "--Building with MsBuild was configured"
    // MSBuildRelease "" "Rebuild" (!!"ExcelProvider.sln") |> ignore
    else
        Trace.log "--Building with dotnet core was configured"

        let setParams (p: DotNet.BuildOptions) =
            { p with
                  Configuration = DotNet.BuildConfiguration.Release }

        DotNet.build setParams "ExcelProvider.sln")

Target.create "RunUnitTests" (fun _ ->
    Trace.log "-- Run the unit tests using test runner"

    let testProj =
        "./tests/ExcelProvider.Tests/ExcelProvider.Tests.fsproj"

    let testOptions (defaults: DotNet.TestOptions) =
        { defaults with
              Configuration = DotNet.BuildConfiguration.Release }


    DotNet.test testOptions testProj)



Target.create "All" ignore

"Clean"
==> "AssemblyInfo"
==> "Build"
==> "RunUnitTests"
==> "All"

Target.runOrDefault "All"
