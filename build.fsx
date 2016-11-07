// include Fake libs
#r "./packages/build/FAKE/tools/FakeLib.dll"
#r "./packages/build/Fantomas/lib/FantomasLib.dll"
#r "./packages/build/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"

open Fake
open Fake.Testing.NUnit3
open Fantomas.FakeHelpers
open Fantomas.FormatConfig


// Directories
let buildDir  = "./build/"
let releaseDir = buildDir
let deployDir = "./deploy/"
let fantomasConfig = FormatConfig.Default

// Filesets
let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir; releaseDir]
)

Target "Build" (fun _ ->
    // compile all projects below src/app/
    MSBuildDebug buildDir "Build" appReferences
    |> Log "AppBuild-Output: "
)

Target "Deploy" (fun _ ->
    !! (releaseDir + "/**/*.*")
    -- "*.zip"
    |> Zip buildDir (deployDir + "/ApplicationName." + version + ".zip")
)

Target "Test" (fun _ ->
    let testDll = !! (buildDir + "/Tests.dll")
    testDll
    |> NUnit3 (fun p ->
        {p with
            ToolPath = "./packages/test/NUnit.ConsoleRunner/tools/nunit3-console.exe"})
)

Target "TestRelease" (fun _ ->
    let testDll = !! (releaseDir + "/Tests.dll")
    testDll
    |> NUnit3 (fun p ->
        {p with
            ToolPath = "./packages/test/NUnit.ConsoleRunner/tools/nunit3-console.exe"})
)


Target "BuildRelease" (fun _ ->
    MSBuildRelease releaseDir "Build" appReferences
    |> Log "AppBuild-Output:"
)

Target "FormatCode" (fun _ ->
    !! "StackVM/**/*.fs"
    ++ "Tests/**/*.fs"
    |> formatCode fantomasConfig
    |> Log "Formatted files: "
)

Target "Release" (ignore)

"Clean"
==> "Build"

"Clean"
==> "BuildRelease"

"TestRelease"
==> "Deploy"

"Build"
==> "Test"

"BuildRelease"
==> "TestRelease"

"TestRelease"
==> "Release"

// start build
RunTargetOrDefault "Build"
