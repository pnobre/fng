namespace PNobre.NortonGuides.Core

open System.IO

/// A single option on a guide menu: its label and the file offset it jumps to.
type MenuPrompt = { Text: string; Offset: int }

/// A menu from the guide's menu bar: a title and its prompts.
type Menu =
    { Title: string
      Prompts: MenuPrompt list }

/// The decoded header of a Norton Guide: title, credit lines, and menu bar.
type Header =
    { Title: string
      Credits: string list
      Menus: Menu list }

/// Parses the fixed header and menu bar that open a `.NG` file.
module Header =

    // Header field sizes (offsets 0..377), per the Norton Guide format.
    [<Literal>]
    let private TitleLength = 40

    [<Literal>]
    let private CreditLines = 5

    [<Literal>]
    let private CreditLength = 66

    // A menu record (XOR-ciphered) is: type(2) size(2) count(2) skip(20),
    // then count-1 prompt offsets, skip (count)*8, the menu title, the prompts,
    // and a trailing byte.
    let private readMenu (r: NgReader) : Menu =
        r.Position <- r.Position + 4 // type marker + byte size
        let prompts = int (r.ReadUInt16(decrypt = true)) - 1
        r.Position <- r.Position + 20

        let offsets = [ for _ in 1..prompts -> r.ReadInt32(decrypt = true) ]
        r.Position <- r.Position + (prompts + 1) * 8

        let title = r.ReadStringZ(decrypt = true)
        let labels = [ for _ in 1..prompts -> r.ReadStringZ(decrypt = true) ]
        r.Position <- r.Position + 1 // trailing byte

        { Title = title
          Prompts = List.map2 (fun text offset -> { Text = text; Offset = offset }) labels offsets }

    /// Decode the header (and menu bar) from a guide image. Returns `Error` for a
    /// non-Norton-Guide file or truncated data rather than throwing.
    let parse (data: byte[]) : Result<Header, string> =
        try
            let r = NgReader(data)

            match r.ReadString 2 with
            | "EH" -> Error "Expert Help (.EH) guides are not supported yet"
            | "NG" ->
                r.Position <- 6 // skip 4 unknown header bytes
                let menuCount = int (r.ReadUInt16())
                let title = r.ReadString TitleLength
                let credits = [ for _ in 1..CreditLines -> r.ReadString CreditLength ]
                let menus = [ for _ in 1..menuCount -> readMenu r ]

                Ok
                    { Title = title
                      Credits = credits
                      Menus = menus }
            | other -> Error $"not a Norton Guide file (magic: {other})"
        with :? EndOfStreamException as ex ->
            Error ex.Message
