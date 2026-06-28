# fng — F# Norton Guides

A modern, cross-platform viewer (and, later, compiler) for classic Norton Guides
(`.NG`) files, built with F# on .NET 10.

> Norton Guides was a TSR reference-database program from the late 1980s. `.NG`
> files were everywhere on BBSs — language references, interrupt lists, hardware
> docs. This project decodes that format and brings it to modern desktops and the web.

## Status

Early development. See [`docs/plan.md`](docs/plan.md) for the roadmap.

- **Phase 1 — Decoder + console dump** *(in progress)*
- Phase 2 — Avalonia desktop viewer (Windows/macOS/Linux)
- Phase 3 — WebAssembly head (Avalonia browser)
- Phase 4 — `.NG` compiler

## Architecture

| Project          | Purpose                                                  |
| ---------------- | -------------------------------------------------------- |
| `Fng.Core`       | Pure F# `.NG` codec — open, decode, navigate, render-to-model. No UI/I-O-dialog deps so it stays WebAssembly-clean. |
| `Fng.Cli`        | Console decoder that dumps a `.NG` file's structure.     |
| `Fng.Build`      | FAKE build pipeline (Format → Lint → Build → Test).      |
| `Fng.Core.Tests` | xUnit + FsCheck tests for the codec.                     |

## Building

Build automation is a **FAKE** project (`src/PNobre.NortonGuides.Build`) targeting
.NET 10. Run it with `dotnet run` — the same command on Windows, macOS and Linux
(no shell scripts):

```sh
dotnet tool restore                                                     # first run: paket, fantomas, fsharplint
dotnet run --project src/PNobre.NortonGuides.Build                      # full pipeline: CheckFormat → Lint → Build → Test
dotnet run --project src/PNobre.NortonGuides.Build -- --target Format   # reformat with Fantomas
```

Requires the .NET 10 SDK. NuGet dependencies are managed with **Paket**. FAKE runs
as a net10 project rather than a `build.fsx` script, because FAKE's script runner
needs the .NET 6 reference pack.
