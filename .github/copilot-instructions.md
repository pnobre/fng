# fng Project Context

F# / .NET 10 viewer (and, later, compiler) for classic Norton Guides (`.NG`) files.

## Standards

Follow the standards in `.claude/`:

- `.claude/fsharp.md` — idiomatic, type-driven F# ("make illegal states unrepresentable").
- `.claude/qa.md` — xUnit + FsCheck, TDD, coverage expectations.
- `.claude/docs.md` — docs, comments, Mermaid diagrams.

Key points: F# 10 on .NET 10; `task { ... }` for async; `Result<'T,'E>` +
FsToolkit.ErrorHandling for errors; DUs/records for the domain; never return `null`.
`PNobre.NortonGuides.Core` stays free of UI and blocking-I/O dependencies so the
WebAssembly head (Phase 3) can reuse it.

## Tooling

- **Paket** manages NuGet dependencies (`paket.dependencies` + per-project `paket.references`).
- **FAKE** drives the build (`src/PNobre.NortonGuides.Build`).
- Anything pushed MUST build, lint (`dotnet fsharplint`), and be formatted (`dotnet fantomas`).
  Use `./build.sh Format` then `./build.sh`.

## Development Process

### Feature workflow

1. Update `main` (`git checkout main && git pull`).
2. Branch (`git checkout -b feat/xyz`).
3. Write unit tests first (TDD).
4. Implement.
5. Update docs in `docs/` and the issue checklist in `docs/plan.md` (`[ ]` → `[/]`).
6. Format + lint (`./build.sh Format`).
7. Verify green (`./build.sh`).
8. Commit, push, open a PR referencing the issue.

### Bug fixing

Same as above on a `fix/xyz` branch, but step 3 is a failing test that reproduces the bug.

### Plan markers

Mark a `docs/plan.md` task `[/]` when you start it. Only mark it `[x]` after the PR is
merged and you sync `main` for the next task.
