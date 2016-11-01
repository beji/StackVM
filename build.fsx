// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing.NUnit3

// Directories
let buildDir  = "./build/"
let releaseDir = "./dist"
let deployDir = "./deploy/"


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

Target "Release" (fun _ ->
    MSBuildRelease releaseDir "Build" appReferences
    |> Log "AppBuild-Output:"
)

"Clean"
==> "Build"

"Release"
==> "Deploy"

"Build"
==> "Test"

"Test"
==> "Release"

// start build
RunTargetOrDefault "Build"
