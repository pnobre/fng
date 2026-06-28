namespace PNobre.NortonGuides.Core

/// Format constants for the Norton Guide (.NG) binary format.
module Codec =

    // ponytail: seed only the single defining constant so the test project has
    // something real to assert against; the full codec lands in #2-#7.

    /// XOR key applied to .NG file content following the header.
    [<Literal>]
    let XorKey = 0x1Auy
