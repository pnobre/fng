module PNobre.NortonGuides.Core.Tests.NgReaderTests

open System.IO
open Xunit
open PNobre.NortonGuides.Core

// The opening bytes of a real Norton Guide header (MOUSE.NG): "NG", three int16s,
// then the title field padded with NULs.
let private mouseHeader =
    [| 0x4Euy
       0x47uy // "NG"
       0x00uy
       0x01uy // int16 LE = 256
       0x00uy
       0x00uy // int16 LE = 0
       0x01uy
       0x00uy // int16 LE = 1
       0x4Duy
       0x6Fuy
       0x75uy
       0x73uy
       0x65uy
       0x20uy // "Mouse "
       0x53uy
       0x65uy
       0x72uy
       0x76uy
       0x69uy
       0x63uy
       0x65uy
       0x73uy // "Services"
       0x00uy
       0x00uy |] // NUL padding

[<Fact>]
let ``ReadString reads the NG magic`` () =
    let r = NgReader(mouseHeader)
    Assert.Equal("NG", r.ReadString 2)

[<Fact>]
let ``Reads advance the position`` () =
    let r = NgReader(mouseHeader)
    r.ReadString 2 |> ignore
    Assert.Equal(2, r.Position)

[<Fact>]
let ``UInt16 is assembled little-endian`` () =
    let r = NgReader(mouseHeader)
    r.Position <- 2
    Assert.Equal(256us, r.ReadUInt16())

[<Fact>]
let ``Int32 is assembled little-endian`` () =
    let r = NgReader([| 0x78uy; 0x56uy; 0x34uy; 0x12uy |])
    Assert.Equal(0x12345678, r.ReadInt32())

[<Fact>]
let ``Position seeks to an absolute offset`` () =
    let r = NgReader(mouseHeader)
    r.Position <- 8
    Assert.Equal("Mouse Services", r.ReadString 16)

[<Fact>]
let ``ReadString cuts a fixed field at the first NUL`` () =
    let r = NgReader([| 0x41uy; 0x42uy; 0x00uy; 0x43uy |]) // "AB\0C"
    Assert.Equal("AB", r.ReadString 4)

[<Fact>]
let ``Decrypt XORs each byte with the NG key`` () =
    // 'A' (0x41) ciphered is 0x41 ^^^ 0x1A = 0x5B
    let r = NgReader([| 0x5Buy |])
    Assert.Equal(0x41uy, r.ReadByte(decrypt = true))

[<Fact>]
let ``Decrypt round-trips ciphered text back to the original`` () =
    let plain = "Norton Guide"B
    let ciphered = plain |> Array.map (fun b -> b ^^^ Codec.XorKey)
    let r = NgReader(ciphered)
    Assert.Equal("Norton Guide", r.ReadString(plain.Length, decrypt = true))

[<Fact>]
let ``Reading past the end raises EndOfStreamException`` () =
    let r = NgReader([| 0x00uy |])
    Assert.Throws<EndOfStreamException>(fun () -> r.ReadInt32() |> ignore) |> ignore
