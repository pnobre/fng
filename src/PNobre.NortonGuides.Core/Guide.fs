namespace PNobre.NortonGuides.Core

open System.IO

/// A fully decoded guide: its header (title, credits, menu bar) and every entry.
type Guide = { Header: Header; Entries: Entry list }

module Guide =

    /// Decode a whole guide image. Returns `Error` for a non-guide, an Expert Help
    /// file, or truncated/corrupt data rather than throwing.
    let parse (data: byte[]) : Result<Guide, string> =
        try
            let r = NgReader data
            let header = Header.read r

            Ok
                { Header = header
                  Entries = Entry.walk r }
        with
        | :? EndOfStreamException as ex -> Error ex.Message
        | :? InvalidDataException as ex -> Error ex.Message

    /// The entry that begins at the given file offset, if any. Menu prompts,
    /// short-entry lines and see-also links all reference entries by this offset.
    // ponytail: linear scan; lookups are user-click-driven, so a Map isn't worth it yet.
    let entryAt (offset: int) (guide: Guide) : Entry option =
        guide.Entries |> List.tryFind (fun e -> e.Offset = offset)
