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

```sh
./build.sh          # full pipeline: CheckFormat → Lint → Build → Test
./build.sh Format   # reformat with Fantomas
```

Requires the .NET 10 SDK. NuGet dependencies are managed with **Paket**; build
automation with **FAKE**.
