module PNobre.NortonGuides.Core.Tests.TextTests

open Xunit
open PNobre.NortonGuides.Core

// Build a one-char string from a raw byte value (for RLE / code-page-437 bytes).
let private chr (b: int) = string (char b)

let private guide =
    match Guide.parse Fixtures.mouseNg with
    | Ok g -> g
    | Error e -> failwith e

[<Fact>]
let ``Plain text is a single normal span`` () =
    let spans = Text.decode "hello"
    Assert.Equal(1, spans.Length)
    Assert.Equal("hello", spans.Head.Text)
    Assert.Equal(Text.normal, spans.Head.Style)

[<Fact>]
let ``Empty line decodes to no spans`` () = Assert.Empty(Text.decode "")

[<Fact>]
let ``RLE expands 0xFF + count into that many spaces`` () =
    Assert.Equal("X   Y", Text.plain ("X" + chr 0xFF + chr 3 + "Y"))

[<Fact>]
let ``Bold toggles on and off around text`` () =
    let spans = Text.decode "a^bB^bc"
    Assert.Equal<string list>([ "a"; "B"; "c" ], spans |> List.map _.Text)
    Assert.Equal<bool list>([ false; true; false ], spans |> List.map (fun s -> s.Style.Bold))

[<Fact>]
let ``Underline and reverse stack on the following text`` () =
    let span = (Text.decode "^u^rx").Head
    Assert.True(span.Style.Underline)
    Assert.True(span.Style.Reverse)

[<Fact>]
let ``Normal resets all attributes`` () =
    let plain = Text.decode "^bbold^nplain" |> List.find (fun s -> s.Text = "plain")
    Assert.Equal(Text.normal, plain.Style)

[<Fact>]
let ``Double caret is a literal caret`` () = Assert.Equal("a^b", Text.plain "a^^b")

[<Fact>]
let ``Caret-C inserts a character by hex code`` () =
    Assert.Equal("AB", Text.plain "^C41^C42") // 0x41='A', 0x42='B'

[<Fact>]
let ``Caret-A sets a colour and repeating it turns colour off`` () =
    let spans = Text.decode "^A1Ehi^A1Ebye"

    let colourOf text =
        (spans |> List.find (fun s -> s.Text = text)).Style.Colour

    Assert.Equal(Some { Foreground = 0xE; Background = 0x1 }, colourOf "hi")
    Assert.Equal(None, colourOf "bye")

[<Fact>]
let ``Code page 437 high bytes map to Unicode`` () =
    // 0xC4 -> U+2500 (box light horizontal), 0xDB -> U+2588 (full block)
    Assert.Equal(chr 0x2500 + chr 0x2588, Text.plain (chr 0xC4 + chr 0xDB))

[<Fact>]
let ``A real bold MOUSE line decodes to a bold span`` () =
    match (guide.Entries |> List.find (fun e -> e.Offset = 1335)).Body with
    | Long(lines, _) ->
        let bold = Text.decode lines.Head |> List.find (fun s -> s.Style.Bold)
        Assert.Contains("INT 33", bold.Text)
    | Short _ -> Assert.Fail "expected a long entry"

[<Fact>]
let ``A short entry line keeps its RLE indentation`` () =
    match guide.Entries.Head.Body with
    | Short lines -> Assert.Equal("     Reset Driver and Read Status", Text.plain lines.Head.Text)
    | Long _ -> Assert.Fail "expected a short entry"
