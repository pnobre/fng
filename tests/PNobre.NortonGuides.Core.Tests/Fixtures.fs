module PNobre.NortonGuides.Core.Tests.Fixtures

open System
open System.IO

/// MOUSE.NG — a real, public-domain Norton Guide committed as a test fixture
/// (its own credits state "No Copyright on this database, it's Public Domain").
let mouseNg =
    File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "MOUSE.NG"))
