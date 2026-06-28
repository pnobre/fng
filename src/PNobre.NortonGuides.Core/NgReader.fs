namespace PNobre.NortonGuides.Core

open System
open System.IO
open System.Text
open System.Buffers.Binary

/// Random-access reader over a Norton Guide image held in memory.
///
/// The header is plaintext while entry data is ciphered with a constant XOR
/// (`Codec.XorKey`), so every primitive read takes an optional `decrypt` flag.
/// Reading past the end raises `EndOfStreamException`; the public open boundary
/// is responsible for turning that into a `Result`.
type NgReader(data: byte[]) =
    let mutable pos = 0

    let read n =
        if n < 0 then
            invalidArg (nameof n) "negative read length"

        if pos + n > data.Length then
            raise (EndOfStreamException $"NG read past end (pos {pos}, need {n}, len {data.Length})")

        let slice = data[pos .. pos + n - 1]
        pos <- pos + n
        slice

    let maybeDecrypt decrypt (bytes: byte[]) =
        if decrypt then
            bytes |> Array.map (fun b -> b ^^^ Codec.XorKey)
        else
            bytes

    /// Current absolute byte offset; assign to seek to an entry offset.
    member _.Position
        with get () = pos
        and set v = pos <- v

    member _.Length = data.Length
    member _.AtEnd = pos >= data.Length

    member _.ReadBytes(count, ?decrypt) =
        read count |> maybeDecrypt (defaultArg decrypt false)

    member this.ReadByte(?decrypt) =
        (this.ReadBytes(1, ?decrypt = decrypt))[0]

    member this.ReadUInt16(?decrypt) =
        BinaryPrimitives.ReadUInt16LittleEndian(ReadOnlySpan(this.ReadBytes(2, ?decrypt = decrypt)))

    member this.ReadInt16(?decrypt) =
        BinaryPrimitives.ReadInt16LittleEndian(ReadOnlySpan(this.ReadBytes(2, ?decrypt = decrypt)))

    member this.ReadInt32(?decrypt) =
        BinaryPrimitives.ReadInt32LittleEndian(ReadOnlySpan(this.ReadBytes(4, ?decrypt = decrypt)))

    /// Read a fixed-width field and cut it at the first NUL (Norton Guides pads with NULs).
    member this.ReadString(length, ?decrypt) =
        let bytes = this.ReadBytes(length, ?decrypt = decrypt)

        let len =
            match Array.tryFindIndex ((=) 0uy) bytes with
            | Some i -> i
            | None -> bytes.Length

        // ponytail: Latin1 is byte-faithful and exact for the ASCII header/menu text;
        // CP437 box-drawing and accented glyphs arrive with the text decoder (#5).
        Encoding.Latin1.GetString(bytes, 0, len)

    /// Read a NUL-terminated string (menu/entry text), consuming the terminator.
    /// Bounded by the end of the buffer, which raises `EndOfStreamException`.
    member this.ReadStringZ(?decrypt) =
        let sb = StringBuilder()
        let mutable b = this.ReadByte(?decrypt = decrypt)

        while b <> 0uy do
            sb.Append(char b) |> ignore
            b <- this.ReadByte(?decrypt = decrypt)

        sb.ToString()
