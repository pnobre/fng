module PNobre.NortonGuides.Core.Tests.SmokeTests

open Xunit
open PNobre.NortonGuides.Core

[<Fact>]
let ``Core is referenced and exposes the NG XOR key`` () = Assert.Equal(0x1Auy, Codec.XorKey)
