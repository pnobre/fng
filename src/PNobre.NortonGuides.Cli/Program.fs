module PNobre.NortonGuides.Cli.Program

open System
open System.IO
open PNobre.NortonGuides.Core

// DOS colour index (0-7) -> ANSI colour offset; DOS and ANSI order the 8 colours
// differently. Indices 8-15 are the bright variants.
let private dosToAnsi = [| 0; 4; 2; 6; 1; 5; 3; 7 |]
let private esc = "\u001b["
let private reset = "\u001b[0m"

let private ansiOf (style: Style) =
    let codes =
        [ if style.Bold then
              yield 1
          if style.Underline then
              yield 4
          if style.Reverse then
              yield 7
          match style.Colour with
          | Some c ->
              yield (if c.Foreground >= 8 then 90 else 30) + dosToAnsi[c.Foreground &&& 7]
              yield (if c.Background >= 8 then 100 else 40) + dosToAnsi[c.Background &&& 7]
          | None -> () ]

    if List.isEmpty codes then
        ""
    else
        esc + (codes |> List.map string |> String.concat ";") + "m"

/// Render a raw line: as-is with `--raw`, ANSI-coloured to a terminal, otherwise plain.
let private renderLine raw colour (line: string) =
    if raw then
        line
    elif not colour then
        Text.plain line
    else
        Text.decode line
        |> List.map (fun s ->
            match ansiOf s.Style with
            | "" -> s.Text
            | a -> a + s.Text + reset)
        |> String.concat ""

let private link offset =
    if offset > 0 then $"  -> @{offset}" else ""

let private dumpEntry raw colour (entry: Entry) =
    match entry.Body with
    | Short lines ->
        printfn $"[@{entry.Offset}] short, {lines.Length} lines"

        for l in lines do
            printfn $"  {renderLine raw colour l.Text}{link l.Offset}"
    | Long(lines, seeAlso) ->
        printfn $"[@{entry.Offset}] long, {lines.Length} lines"

        for l in lines do
            printfn $"  {renderLine raw colour l}"

        if not (List.isEmpty seeAlso) then
            printfn "  See also:"

            for s in seeAlso do
                printfn $"    {s.Text}{link s.Offset}"

    printfn ""

let private dump raw colour (guide: Guide) =
    printfn $"Title:   {guide.Header.Title}"

    guide.Header.Credits
    |> List.filter (fun c -> c.Trim() <> "")
    |> List.iter (fun c -> printfn $"         {c.Trim()}")

    printfn ""

    for menu in guide.Header.Menus do
        printfn $"Menu: {menu.Title}"

        for p in menu.Prompts do
            printfn $"  {p.Text}{link p.Offset}"

        printfn ""

    printfn $"Entries: {guide.Entries.Length}"
    printfn ""

    for entry in guide.Entries do
        dumpEntry raw colour entry

[<EntryPoint>]
let main argv =
    let raw = argv |> Array.contains "--raw"
    let paths = argv |> Array.filter (fun a -> not (a.StartsWith "--"))

    match paths with
    | [| path |] when not (File.Exists path) ->
        eprintfn $"fng: file not found: {path}"
        2
    | [| path |] ->
        match Guide.parse (File.ReadAllBytes path) with
        | Ok guide ->
            dump raw (not Console.IsOutputRedirected) guide
            0
        | Error e ->
            eprintfn $"fng: cannot read {path}: {e}"
            1
    | _ ->
        eprintfn "usage: fng [--raw] <file.ng>"
        1
