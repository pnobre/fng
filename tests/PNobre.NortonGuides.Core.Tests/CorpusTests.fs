module PNobre.NortonGuides.Core.Tests.CorpusTests

open System
open System.IO
open Xunit
open PNobre.NortonGuides.Core

/// The guides to sweep, taken from the FNG_CORPUS environment variable (a directory
/// of real `.NG` files). Unset means the sweep cannot run, so fail loudly.
let guides: obj[] seq =
    match Environment.GetEnvironmentVariable "FNG_CORPUS" with
    | null
    | "" -> failwith "FNG_CORPUS is not set; point it at a directory of .NG guides to run the corpus sweep"
    | dir -> seq { for file in Directory.GetFiles(dir, "*.NG") -> [| box file |] }

[<Theory>]
[<MemberData(nameof guides)>]
let ``Every guide parses and decodes without throwing`` (path: string) =
    // A throw here fails the test; a clean Error result is acceptable.
    match Guide.parse (File.ReadAllBytes path) with
    | Error _ -> ()
    | Ok guide ->
        for entry in guide.Entries do
            let lines =
                match entry.Body with
                | Short links -> links |> List.map _.Text
                | Long(text, _) -> text

            for line in lines do
                Text.decode line |> ignore
