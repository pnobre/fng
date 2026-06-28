# Documentation Standards

## Scope

- Update `README.md` whenever a new feature lands or existing behaviour changes.
- Keep `docs/` in sync with the code; stale documentation is worse than no documentation.
- Include documentation changes in the same commit/PR as the code they describe.

## Code comments

- Comments explain *why*, never *what*. Well-named identifiers already communicate what the code does.
- One short line maximum. No multi-paragraph docstrings or block comment walls.
- Use `///` XML doc comments only on public API surface (types, module-level functions) where the intent is not obvious from the signature.

## Diagrams

- Use **Mermaid.js** for all diagrams — flowcharts, sequence diagrams, class diagrams, state machines. This keeps them version-control-friendly.
- Place diagrams in `docs/` or inline in the relevant `README.md` section.
- Label every node and edge; a diagram without context is decoration, not documentation.

## Markdown

- Use GitHub-flavoured Markdown.
- Add a Table of Contents to files longer than four sections.
- Validate all cross-file links before committing.
- Use fenced code blocks with language tags (` ```fsharp `, ` ```xml `, ` ```http `).

## API / protocol documentation

- Document any HTTP endpoint with: method, path, query params, request body (if any), response shape, and example.
- Document any XML format (Atom feed, OPDS catalogue) with the expected element structure and a short valid example.
- Reference the relevant RFC or specification section when implementing protocol behaviour.
