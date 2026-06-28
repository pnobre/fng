namespace PNobre.NortonGuides.Core

open System
open System.Text

/// A DOS text-mode colour attribute: a foreground and background index (0-15).
type Colour = { Foreground: int; Background: int }

/// The styling in force over a run of text.
type Style =
    { Bold: bool
      Underline: bool
      Reverse: bool
      Colour: Colour option }

/// A run of text that shares one style — the renderer-agnostic unit of decoded
/// entry text.
type Span = { Text: string; Style: Style }

/// Expands a raw guide line into styled spans: RLE space runs, `^` control codes
/// (bold/underline/reverse/colour/normal, literal caret, literal char), and the
/// DOS code page 437 mapping for box-drawing and accented characters.
module Text =

    /// The neutral style (no attributes), and the starting point for every line.
    let normal =
        { Bold = false
          Underline = false
          Reverse = false
          Colour = None }

    // Code page 437, high half (bytes 0x80-0xFF) as Unicode code points. Bytes
    // below 0x80 are ASCII. RLE (0xFF) and control (0x5E) bytes are consumed before
    // this mapping, so they never reach it.
    let private cp437High =
        [| 0x00C7
           0x00FC
           0x00E9
           0x00E2
           0x00E4
           0x00E0
           0x00E5
           0x00E7
           0x00EA
           0x00EB
           0x00E8
           0x00EF
           0x00EE
           0x00EC
           0x00C4
           0x00C5
           0x00C9
           0x00E6
           0x00C6
           0x00F4
           0x00F6
           0x00F2
           0x00FB
           0x00F9
           0x00FF
           0x00D6
           0x00DC
           0x00A2
           0x00A3
           0x00A5
           0x20A7
           0x0192
           0x00E1
           0x00ED
           0x00F3
           0x00FA
           0x00F1
           0x00D1
           0x00AA
           0x00BA
           0x00BF
           0x2310
           0x00AC
           0x00BD
           0x00BC
           0x00A1
           0x00AB
           0x00BB
           0x2591
           0x2592
           0x2593
           0x2502
           0x2524
           0x2561
           0x2562
           0x2556
           0x2555
           0x2563
           0x2551
           0x2557
           0x255D
           0x255C
           0x255B
           0x2510
           0x2514
           0x2534
           0x252C
           0x251C
           0x2500
           0x253C
           0x255E
           0x255F
           0x255A
           0x2554
           0x2569
           0x2566
           0x2560
           0x2550
           0x256C
           0x2567
           0x2568
           0x2564
           0x2565
           0x2559
           0x2558
           0x2552
           0x2553
           0x256B
           0x256A
           0x2518
           0x250C
           0x2588
           0x2584
           0x258C
           0x2590
           0x2580
           0x03B1
           0x00DF
           0x0393
           0x03C0
           0x03A3
           0x03C3
           0x00B5
           0x03C4
           0x03A6
           0x0398
           0x03A9
           0x03B4
           0x221E
           0x03C6
           0x03B5
           0x2229
           0x2261
           0x00B1
           0x2265
           0x2264
           0x2320
           0x2321
           0x00F7
           0x2248
           0x00B0
           0x2219
           0x00B7
           0x221A
           0x207F
           0x00B2
           0x25A0
           0x00A0 |]

    let private mapByte (b: int) =
        if b < 0x80 then char b else char cp437High[b - 0x80]

    let private hexDigit c =
        if c >= '0' && c <= '9' then Some(int c - int '0')
        elif c >= 'a' && c <= 'f' then Some(int c - int 'a' + 10)
        elif c >= 'A' && c <= 'F' then Some(int c - int 'A' + 10)
        else None

    let private hexByte (a: char) (b: char) =
        match hexDigit a, hexDigit b with
        | Some hi, Some lo -> Some(hi * 16 + lo)
        | _ -> None

    /// Decode one raw entry line into styled spans.
    let decode (line: string) : Span list =
        let spans = ResizeArray<Span>()
        let sb = StringBuilder()
        let mutable style = normal
        let n = line.Length
        let mutable i = 0

        let flush () =
            if sb.Length > 0 then
                spans.Add { Text = sb.ToString(); Style = style }
                sb.Clear() |> ignore

        let restyle s =
            if s <> style then
                flush ()
                style <- s

        while i < n do
            let c = int line[i]

            if c = 0xFF && i + 1 < n then
                // RLE: the next byte is a count of spaces (0xFF count means a single space).
                let count = int line[i + 1]
                sb.Append(' ', (if count = 0xFF then 1 else count)) |> ignore
                i <- i + 2
            elif c = 0x5E && i + 1 < n then // '^' control code (letters are case-insensitive)
                match Char.ToLower line[i + 1] with
                | '^' ->
                    sb.Append('^') |> ignore
                    i <- i + 2
                | 'b' ->
                    restyle { style with Bold = not style.Bold }
                    i <- i + 2
                | 'u' ->
                    restyle
                        { style with
                            Underline = not style.Underline }

                    i <- i + 2
                | 'r' ->
                    restyle
                        { style with
                            Reverse = not style.Reverse }

                    i <- i + 2
                | 'n' ->
                    restyle normal
                    i <- i + 2
                | 'a' when i + 3 < n ->
                    match hexByte line[i + 2] line[i + 3] with
                    | Some attr ->
                        let colour =
                            { Foreground = attr &&& 0xF
                              Background = (attr >>> 4) &&& 0xF }
                        // Repeating the current colour turns colour mode back off.
                        restyle
                            { style with
                                Colour = (if style.Colour = Some colour then None else Some colour) }

                        i <- i + 4
                    | None ->
                        sb.Append('^') |> ignore
                        i <- i + 1
                | 'c' when i + 3 < n ->
                    match hexByte line[i + 2] line[i + 3] with
                    | Some code ->
                        sb.Append(mapByte code) |> ignore
                        i <- i + 4
                    | None ->
                        sb.Append('^') |> ignore
                        i <- i + 1
                | _ ->
                    sb.Append('^') |> ignore
                    i <- i + 1
            else
                sb.Append(mapByte c) |> ignore
                i <- i + 1

        flush ()
        List.ofSeq spans

    /// The decoded text of a line with styling stripped (handy for search/plain output).
    let plain (line: string) : string =
        decode line |> List.map _.Text |> String.concat ""
