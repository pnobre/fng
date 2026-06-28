module Program

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet

// Resolve the repo root relative to this project so targets work regardless of
// the working directory the runner is invoked from.
let private repoRoot =
    System.IO.Path.GetFullPath(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", ".."))

let private run cmd args =
    let result = DotNet.exec (fun o -> { o with WorkingDirectory = repoRoot }) cmd args

    if not result.OK then
        failwithf $"'dotnet %s{cmd} %s{args}' failed with exit code %d{result.ExitCode}"

[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    Target.create "Format" (fun _ -> run "fantomas" "src tests")

    Target.create "CheckFormat" (fun _ -> run "fantomas" "src tests --check")

    Target.create "Lint" (fun _ -> run "fsharplint" "lint --config fsharplint.json PNobre.NortonGuides.slnx")

    Target.create "Build" (fun _ -> run "build" "--configuration Release PNobre.NortonGuides.slnx")

    Target.create "Test" (fun _ -> run "test" "--configuration Release --no-build PNobre.NortonGuides.slnx")

    Target.create "All" ignore

    "CheckFormat" ==> "Lint" ==> "Build" ==> "Test" ==> "All" |> ignore

    Target.runOrDefaultWithArguments "All"
    0
