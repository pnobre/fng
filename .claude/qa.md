# QA Standards

## Test framework

- **xUnit** for unit and integration tests. Test projects follow the `*.Tests` naming convention.
- **FsCheck** (via `FsCheck.Xunit`) for property-based tests. Use `[<Property>]` attribute directly on test functions.
- No NUnit, no MSTest.

## Coverage expectations

- Every public function and module must have tests before a PR is merged.
- Cover all DU cases, record transitions, and edge conditions in the domain model.
- Cover complete request-to-response flows for HTTP endpoints.

## Test naming

Use plain English names that read as specifications:

```fsharp
[<Fact>]
let ``AtomFeed ToXml includes updated element`` () = ...

[<Fact>]
let ``Entry with no authors serialises without author elements`` () = ...

[<Property>]
let ``Feed round-trips through XML serialisation`` (feed: AtomFeed) = ...
```

## Unit tests

- One assertion per logical scenario (multiple `Assert` calls are fine when verifying a single behaviour).
- Arrange / Act / Assert structure, no blank-line exceptions.
- Use `Assert.Equal`, `Assert.True`, `Assert.Contains` etc. from xUnit directly — no wrapper libraries.
- For XML output tests, parse the output with `XDocument.Parse` or `XElement.Parse` and assert element values, not raw string equality.

```fsharp
[<Fact>]
let ``Person ToXml emits name element`` () =
    let person = { Person.Empty with Name = "John Doe" }
    let xml = Person.ToXml "author" person :?> XElement
    Assert.Equal("John Doe", xml.Element(XName.Get("name", atomNs)).Value)
```

## Property-based tests

Use FsCheck for invariants that should hold across a wide range of inputs:

- Serialisation round-trips (produce XML → parse → check field values).
- Feed structure invariants (every feed has an `id` element, every entry has `title` and `updated`).
- Protocol compliance properties (link `href` is always a non-empty string).

```fsharp
open FsCheck
open FsCheck.Xunit

[<Property>]
let ``Link href survives XML round-trip`` (NonEmptyString href) =
    let link = Link.empty href
    let xml = Link.ToXml link :?> XElement
    xml.Attribute(XName.Get "href").Value = href
```

## Integration tests

- At least one integration test per HTTP endpoint: issue a real request against the Giraffe app hosted in-process and assert the response status and body.
- At least one integration test per Calibre monitor behaviour: verify that library changes are detected and reflected correctly.

## Security / adversarial tests

- Test XML output with inputs containing special characters (`<`, `>`, `&`, `"`, `'`). The serialiser must escape them; never raw-inject user strings into XML.
- Test that path traversal strings in Calibre library paths are rejected.
- Test that malformed or oversized inputs to any parser are handled gracefully (return `Error`, never throw unhandled exceptions).

## Workflow

1. Write failing tests first (TDD).
2. Implement to make them pass.
3. Run the full test suite: `dotnet test`.
4. Refactor only after green.
5. Run `dotnet fantomas` and `dotnet fsharplint` before committing.
