namespace PNobre.NortonGuides.Core

/// Where an entry sits relative to its neighbours and its parent menu/entry.
/// A value of -1 (and sometimes 0) means "none".
type EntryLinks =
    { Previous: int
      Next: int
      ParentOffset: int
      ParentLine: int
      ParentMenu: int
      ParentPrompt: int }

/// The two kinds of guide entry. A short entry is a list of links (a sub-menu);
/// a long entry is help text with optional see-also cross-references. Line text
/// is the raw (still RLE-compressed) string — the text decoder (#5) expands it.
type EntryBody =
    | Short of lines: Link list
    | Long of lines: string list * seeAlso: Link list

/// One decoded entry, tagged with the file offset it starts at (so links that
/// target this offset can be resolved).
type Entry =
    { Offset: int
      Links: EntryLinks
      Body: EntryBody }

/// Reads the chain of entries that follows the header and menu bar.
module Entry =

    [<Literal>]
    let private ShortType = 0

    [<Literal>]
    let private LongType = 1

    // See-also lists are capped in the format.
    [<Literal>]
    let private MaxSeeAlso = 20

    let private link text offset = { Text = text; Offset = offset }

    // The reader is positioned just past the 2-byte type marker; `start` is the
    // entry's own offset and `typ` the marker that was read.
    let private readOne (r: NgReader) (start: int) (typ: int) : Entry =
        r.Position <- r.Position + 2 // size (entries are walked sequentially, not by size)
        let lineCount = int (r.ReadUInt16(decrypt = true))
        let hasSeeAlso = int (r.ReadUInt16(decrypt = true))

        // Read the navigation fields into locals in binary order: record-literal field
        // order is the declaration order, which would otherwise reorder these reads.
        let parentLine = int (r.ReadInt16(decrypt = true))
        let parentOffset = r.ReadInt32(decrypt = true)
        let parentMenu = int (r.ReadInt16(decrypt = true))
        let parentPrompt = int (r.ReadInt16(decrypt = true))
        let previous = r.ReadInt32(decrypt = true)
        let next = r.ReadInt32(decrypt = true)

        let links =
            { Previous = previous
              Next = next
              ParentOffset = parentOffset
              ParentLine = parentLine
              ParentMenu = parentMenu
              ParentPrompt = parentPrompt }

        let body =
            if typ = ShortType then
                // Each line: 2 skipped bytes then its target offset, then all the texts.
                let offsets =
                    List.init lineCount (fun _ ->
                        r.Position <- r.Position + 2
                        r.ReadInt32(decrypt = true))

                let texts = List.init lineCount (fun _ -> r.ReadStringZ(decrypt = true))
                Short(List.map2 link texts offsets)
            else
                let texts = List.init lineCount (fun _ -> r.ReadStringZ(decrypt = true))

                let seeAlso =
                    if hasSeeAlso > 0 then
                        let count = min (int (r.ReadUInt16(decrypt = true))) MaxSeeAlso
                        let offsets = List.init count (fun _ -> r.ReadInt32(decrypt = true))
                        let texts = List.init count (fun _ -> r.ReadStringZ(decrypt = true))
                        r.Position <- r.Position + 1 // trailing byte (see-also mirrors the menu record)
                        List.map2 link texts offsets
                    else
                        []

                Long(texts, seeAlso)

        { Offset = start
          Links = links
          Body = body }

    /// Walk every entry from the reader's current position (the first entry) until
    /// a non-entry marker or the end of the file.
    let walk (r: NgReader) : Entry list =
        [ let mutable go = true

          while go && r.Position + 2 <= r.Length do
              let start = r.Position

              match int (r.ReadUInt16(decrypt = true)) with
              | ShortType -> yield readOne r start ShortType
              | LongType -> yield readOne r start LongType
              | _ -> go <- false ]
