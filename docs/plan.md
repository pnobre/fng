# fng roadmap

Status markers: `[ ]` todo · `[/]` in progress · `[x]` done (set on PR merge).
Each task maps to a GitHub issue.

## Phase 1 — Decoder + console dump

- [/] #1 Project scaffolding (solution, Paket, FAKE, tools)
- [/] #2 XOR-decrypting binary reader (`0x1A`)
- [ ] #3 Header parser (magic, menus, title, credits)
- [ ] #4 Entry model + chain walker (short/long)
- [ ] #5 Text decoder (RLE spaces + control codes)
- [ ] #6 CLI console dump (header, menus, entries)
- [ ] #7 Corpus sweep test (parse all ~60 guides)

## Phase 2 — Avalonia desktop viewer

- [ ] #8 Epic: cross-platform desktop viewer over `Fng.Core`

## Phase 3 — WebAssembly head

- [ ] #9 Epic: Avalonia browser head reusing Core + viewmodels

## Phase 4 — `.NG` compiler

- [ ] #10 Epic: source → `.NG`, validated by the `MOUSE.TXT`+`MOUSE.CTL` → `MOUSE.NG` golden fixture
