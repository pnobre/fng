module PNobre.NortonGuides.Cli.Program

[<EntryPoint>]
let main argv =
    match argv with
    | [| path |] ->
        printfn $"fng: decoding {path} — not implemented yet (see issue #6)"
        0
    | _ ->
        eprintfn "usage: fng <file.ng>"
        1
