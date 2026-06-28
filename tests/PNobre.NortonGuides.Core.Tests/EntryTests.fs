module PNobre.NortonGuides.Core.Tests.EntryTests

open Xunit
open PNobre.NortonGuides.Core

let private guide =
    match Guide.parse Fixtures.mouseNg with
    | Ok g -> g
    | Error e -> failwith $"parse failed: {e}"

let private isShort entry =
    match entry.Body with
    | Short _ -> true
    | Long _ -> false

[<Fact>]
let ``Walks every entry in MOUSE.NG`` () = Assert.Equal(30, guide.Entries.Length)

[<Fact>]
let ``MOUSE.NG has two short entries and the rest are long`` () =
    let shorts = guide.Entries |> List.filter isShort
    Assert.Equal(2, shorts.Length)
    Assert.Equal(28, guide.Entries.Length - shorts.Length)

[<Fact>]
let ``The first entry is a short menu whose lines link to long entries`` () =
    let first = guide.Entries.Head
    Assert.Equal(488, first.Offset)

    match first.Body with
    | Short lines ->
        Assert.Equal(24, lines.Length)
        Assert.Equal(1335, lines.Head.Offset)
        Assert.Contains("Reset Driver", lines.Head.Text)
    | Long _ -> Assert.Fail "expected a short entry"

[<Fact>]
let ``A short line's offset points to a real long entry`` () =
    let target = guide.Entries |> List.find (fun e -> e.Offset = 1335)

    match target.Body with
    | Long(lines, _) -> Assert.Contains("INT 33", lines.Head)
    | Short _ -> Assert.Fail "expected a long entry at offset 1335"

[<Fact>]
let ``The top-level entry has no parent`` () =
    Assert.Equal(-1, guide.Entries.Head.Links.ParentOffset)

[<Fact>]
let ``Every real link target resolves to an entry`` () =
    let offsets = guide.Entries |> List.map (fun e -> e.Offset) |> Set.ofList

    let targets =
        guide.Entries
        |> List.collect (fun e ->
            match e.Body with
            | Short lines -> lines
            | Long(_, seeAlso) -> seeAlso)
        |> List.map (fun l -> l.Offset)
        |> List.filter (fun o -> o > 0) // -1/0 mean "no link"

    Assert.All(targets, (fun o -> Assert.True(offsets.Contains o, $"offset {o} did not resolve to an entry")))

[<Fact>]
let ``Corrupt data is an error, not an exception`` () =
    match Guide.parse (Array.create 400 0xFFuy) with
    | Error _ -> ()
    | Ok _ -> Assert.Fail "expected Error for corrupt data"
