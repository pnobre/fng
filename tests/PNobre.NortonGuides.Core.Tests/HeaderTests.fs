module PNobre.NortonGuides.Core.Tests.HeaderTests

open System
open System.Text
open Xunit
open PNobre.NortonGuides.Core

// A minimal 378-byte header with no menus, for exercising the fixed fields.
let private headerBytes (title: string) (credits: string list) =
    let buf = Array.zeroCreate<byte> 378
    buf[0] <- 0x4Euy // 'N'
    buf[1] <- 0x47uy // 'G'  (menu count at offset 6 stays 0)

    let put off (s: string) =
        Encoding.ASCII.GetBytes(s).CopyTo(buf, (off: int))

    put 8 title
    credits |> List.iteri (fun i c -> put (48 + i * 66) c)
    buf

// A real Norton Guide, committed as a public-domain fixture (its own credits state
// "No Copyright on this database, it's Public Domain"). Reused by later entry tests.
let private mouseNg =
    IO.File.ReadAllBytes(IO.Path.Combine(AppContext.BaseDirectory, "fixtures", "MOUSE.NG"))

let private parseOk data =
    match Header.parse data with
    | Ok h -> h
    | Error e -> failwith $"expected Ok, got Error: {e}"

[<Fact>]
let ``Header reads the title`` () =
    let h = parseOk (headerBytes "Mouse Services" [])
    Assert.Equal("Mouse Services", h.Title)

[<Fact>]
let ``Header reads all five credit lines`` () =
    let h = parseOk (headerBytes "T" [ "one"; "two"; "three"; "four"; "five" ])
    Assert.Equal<string list>([ "one"; "two"; "three"; "four"; "five" ], h.Credits)

[<Fact>]
let ``A file that is not a guide is an error`` () =
    match Header.parse (Array.create 378 0uy) with
    | Error _ -> ()
    | Ok _ -> Assert.Fail "expected Error for bad magic"

[<Fact>]
let ``Expert Help guides are rejected for now`` () =
    let buf = headerBytes "x" []
    buf[0] <- 0x45uy // 'E'
    buf[1] <- 0x48uy // 'H'

    match Header.parse buf with
    | Error e -> Assert.Contains("Expert Help", e)
    | Ok _ -> Assert.Fail "expected Error for EH magic"

[<Fact>]
let ``Truncated data is an error, not an exception`` () =
    match Header.parse [| 0x4Euy; 0x47uy |] with
    | Error _ -> ()
    | Ok _ -> Assert.Fail "expected Error for truncated data"

[<Fact>]
let ``MOUSE.NG header parses title and credits`` () =
    let h = parseOk mouseNg
    Assert.Equal("Mouse Services", h.Title)
    Assert.Equal("    Mouse.NG 1.01 updated May 28, 1988 by Howard Kapustein", h.Credits[0])

[<Fact>]
let ``MOUSE.NG has one menu named Mouse`` () =
    let h = parseOk mouseNg
    Assert.Equal(1, h.Menus.Length)
    Assert.Equal("Mouse", h.Menus[0].Title)

[<Fact>]
let ``MOUSE.NG menu prompts carry their labels and jump offsets`` () =
    let menu = (parseOk mouseNg).Menus[0]
    Assert.Equal<string list>([ "Int 51 Services"; "Definitions"; "Help" ], menu.Prompts |> List.map _.Text)
    Assert.Equal<int list>([ 488; 7786; 10065 ], menu.Prompts |> List.map _.Offset)
