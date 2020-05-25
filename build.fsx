#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"




open Fake.Core
open Fake.IO



Target.create "Clean" (fun _ ->
    Fake.Core.Trace.log "--Cleaning 'bin' and 'temp' directories"
    Shell.cleanDirs [ "bin"; "temp" ]

    )

Target.runOrDefault "Clean"
