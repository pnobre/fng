# F# Coding Standards

Idiomatic, type-driven F# following the "Make Illegal States Unrepresentable" principle.

## Type-driven design

- Model the domain with Discriminated Unions and Records. Avoid primitive obsession — wrap raw strings, ints, and URIs in named types where the domain has constraints.
- Design types so invalid states cannot be expressed. A `LinkRel` DU is better than a raw string that might be empty or malformed.
- Organise code into modules that mirror domain structure (`Atom`, `Opds`, etc.). Companion modules share the name of the type they extend.

## Immutability and safety

- Default to immutable data. Only reach for `mutable` on proven performance-critical hot paths.
- Use `Option<'T>` for optionality and `Result<'T, 'E>` for error paths. Never return `null`.
- Do not use the legacy `Choice` type.

## Async and concurrency

- Use `task { ... }` for all async operations. Do not use `async { ... }` unless an external library forces it.
- Keep the entire call chain async when I/O is involved — no `.Result` or `.GetAwaiter().GetResult()` blocking.

## Error handling

- Use **FsToolkit.ErrorHandling** for monadic composition: `taskResult { ... }`, `result { ... }`, `option { ... }`.
- Use `validation` CE or applicative operators (`<!>`, `<*>`) when collecting multiple independent errors.

## Syntax and style

- Array/list indexing: use `arr[i]`, not `arr.[i]`.
- String formatting: use interpolated strings (`$"..."`) instead of `sprintf` or format specifiers.
- `printfn`: use `printfn $"Value: {v}"` not `printfn "Value: %s" v`.
- Deep record updates: prefer `{ state with Nested.Field = value }` over multiple nested `with` blocks.
- Qualifiers: do not repeat qualifiers for already-opened namespaces. If `open System.Xml.Linq` is in scope, write `XElement`, not `Linq.XElement`.
- Favour pipeline style (`|>`) for data transformations; use point-free only when it reads clearly.
- Prefer `List` functions over explicit recursion for standard collection operations.

## Code hygiene

- Remove all unused `open` statements before committing.
- Remove commented-out code. If something is temporarily disabled, add a TODO comment with a clear reason.
- Run `dotnet fantomas` (2-space indent) before committing.
- Run `dotnet fsharplint` and resolve all warnings before committing.

## Patterns

### Smart value objects

```fsharp
type MediaType = private MediaType of string

module MediaType =
    let create (s: string) =
        if s.Contains('/') then Ok (MediaType s)
        else Error $"Invalid media type: {s}"

    let value (MediaType s) = s
```

### Companion module

```fsharp
type Link = { Href: string; Rel: string option }

module Link =
    let empty href = { Href = href; Rel = None }
    let withRel rel link = { link with Rel = Some rel }
```

### Computation expression builder

```fsharp
type FeedBuilder() =
    member _.Yield _ = AtomFeed.empty "" (DateTimeOffset.UtcNow)

    [<CustomOperation("id")>]
    member _.Id(feed, id) = { feed with Id = id }

    [<CustomOperation("title")>]
    member _.Title(feed, title) = { feed with Title = TextString title }

let atomFeed = FeedBuilder()
```

### Task-based result composition

```fsharp
open FsToolkit.ErrorHandling

let loadCatalogue (path: string) : Task<Result<Catalogue, string>> = taskResult {
    let! raw = File.ReadAllTextAsync(path) // Task<string>
    let! parsed = parseCatalogue raw       // Result<Catalogue, string>
    return parsed
}
```
