# fng roadmap

Status markers: `[ ]` todo · `[/]` in progress · `[x]` done (set on PR merge).
Each task maps to a GitHub issue.

## Phase 1 — Decoder + console dump

- [x] #1 Project scaffolding (solution, Paket, FAKE, tools)
- [x] #2 XOR-decrypting binary reader (`0x1A`)
- [x] #3 Header parser (magic, menus, title, credits)
- [x] #4 Entry model + chain walker (short/long)
- [x] #5 Text decoder (RLE spaces + control codes)
- [x] #6 CLI console dump (header, menus, entries)
- [/] #7 Corpus sweep test (parse all ~60 guides)

## Phase 2 — Avalonia desktop viewer

- [/] #8 Epic: cross-platform desktop viewer over `Fng.Core` (Avalonia.FuncUI + Elmish)
  - [x] #19 App project scaffolding (Avalonia + FuncUI + Elmish)
  - [x] #20 MVU shell: open a guide
  - [x] #21 Three-pane layout (navigation / list / content)
  - [/] #22 Span → Avalonia rendering (styles + NG colour theme)
  - [ ] #23 Navigation: follow the entry offset graph

## Phase 3 — WebAssembly head

- [ ] #9 Epic: Avalonia browser head reusing Core + viewmodels

## Phase 4 — `.NG` compiler

- [ ] #10 Epic: source → `.NG`, validated by the `MOUSE.TXT`+`MOUSE.CTL` → `MOUSE.NG` golden fixture
