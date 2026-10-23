# Mecesoft.Result

Lightweight generic Result wrapper for .NET with optional pagination helpers (EF Core friendly).

- Target Frameworks: `net8.0`, `net9.0`, `net10.0`
- Package: `Mecesoft.Result` 1.1.0
- EF Core: 8.0.32 / 9.0.21 / 10.0.13 (matching each target)
- Repository: https://github.com/Mecesoft/Mecesoft.Result
- License: MIT

Türkçe: .NET için hafif bir Result sarmalayıcısı. İsteğe bağlı sayfalama (pagination) bilgisi taşır ve EF Core ile uyumludur.

## Support status

Version 1.1.0 supports **.NET 8, .NET 9 and .NET 10**. CI builds all three target
frameworks and runs the characterization tests independently on the .NET 8, .NET 9 and
.NET 10 runtimes. CI does not publish packages or deploy anything.

.NET 9 has been out of Microsoft support since May 2026; the `net9.0` target remains for
existing consumers. .NET 8 support ends on 10 November 2026. .NET 10 is the current LTS.

| Target framework | EF Core dependency |
|---|---|
| `net8.0` | `Microsoft.EntityFrameworkCore` 8.0.32 |
| `net9.0` | `Microsoft.EntityFrameworkCore` 9.0.21 |
| `net10.0` | `Microsoft.EntityFrameworkCore` 10.0.13 |

## Install

NuGet Package Manager:

```
Install-Package Mecesoft.Result
```

.NET CLI:

```
dotnet add package Mecesoft.Result
```

## Quick start

Return success results:

```csharp
using Mecesoft.Result;

Result<string> ok = Result<string>.Succeed("Hello World");
// or via implicit conversion
Result<string> ok2 = "Hello World";
```

Return failures:

```csharp
using Mecesoft.Result;
using System.Net;

var fail1 = Result<string>.Failure((int)HttpStatusCode.BadRequest, "Invalid input");
var fail2 = Result<string>.Failure("Unexpected error");
// or via implicit tuple conversion
Result<string> fail3 = ((int)HttpStatusCode.NotFound, "Not found");
```

## Pagination with EF Core

```csharp
using Mecesoft.Result;
using Microsoft.EntityFrameworkCore;

// IQueryable source (e.g., DbSet<User>)
var paged = await dbContext.Users
    .Where(u => u.IsActive)
    .OrderBy(u => u.Id)
    .ToPagedListAsync(pageNumber: 1, pageSize: 10, cancellationToken);

// Wrap into a Result
var result = Result<PaginationResult<User>>.Succeed(paged);

// Or return items with separate pagination metadata
var items = paged.Datas;               // IList<User>?
var pagination = new PaginationResultBase(paged.PageNumber, paged.PageSize, paged.TotalCount);
var result2 = Result<IList<User>>.Succeed(items!, pagination);
```

## Pagination behavior in 1.1.0

- `pageNumber < 1` is normalized to `1`; `pageSize < 1` is normalized to `10`.
- When the filtered query is empty, `Datas` is `null` and the requested
  `PageNumber`/`PageSize` are preserved in the metadata.
- When the requested page is beyond the last page, `Datas` is an empty list and the
  requested page metadata is preserved.
- Page offsets use `long` arithmetic, so large `pageNumber`/`pageSize` values cannot
  overflow the internal Skip calculation.

## API overview

- Result<T>
  - Properties: Data, Pagination, ErrorMessages, IsSuccessful, StatusCode (JsonIgnore)
  - Factories: Succeed(...), Failure(...)
  - Implicit conversions from T and from (statusCode, errorMessage[s])
- PaginationResultBase: PageNumber, PageSize, TotalPages, TotalCount, IsFirstPage, IsLastPage
- PaginationResult<T>: Datas + all fields above
- PaginationMeta: PageNumber, PageSize, TotalRecords, TotalPages, HasNextPage, HasPreviousPage; Create(...)
- IQueryable extension: ToPagedListAsync(pageNumber, pageSize, cancellationToken)

## Notes

- `StatusCode` is ignored by `System.Text.Json` because of the `[JsonIgnore]` attribute.
  Newtonsoft.Json does not honor that attribute and will serialize `StatusCode` unless it
  is configured separately.
- If `pageSize < 1` or `pageNumber < 1`, defaults are applied (10 and 1 respectively).
  Empty queries preserve those normalized values in the returned metadata.

## What changed in 1.1.0

- Added `net8.0` and `net10.0` target frameworks alongside `net9.0`.
- Added matching EF Core 8/9/10 dependency groups.
- Fixed pagination offset overflow by using `long` arithmetic.
- Fixed empty-result pagination metadata while preserving `Datas = null`.
- Fixed `PaginationMeta.HasPreviousPage` for empty data sets.
- Public types, method signatures, implicit conversions and JSON property contract are
  unchanged from 1.0.2.

## Known limitations in 1.1.0

These limitations describe existing behavior; they are not claims that the behavior is
desirable.

- **EF Core only:** `ToPagedListAsync` requires an EF Core async query provider. Other
  `IQueryable` implementations are not supported.
- **No tracking by default:** `ToPagedListAsync` applies `AsNoTracking()` unconditionally.
  Returned entities are detached and updates will not be tracked by the source
  `DbContext`. Callers that need tracking must load data another way.
- **Ordering is caller-owned:** the extension does not add `OrderBy`. Always provide a
  deterministic ordering before paging; otherwise row order is undefined.
- **No page-size ceiling:** `pageSize` has no upper bound. Validate or cap user-supplied
  values in the calling application.
- **`int` total count:** totals are returned as `int`; result sets larger than
  `int.MaxValue` are not supported by the pagination metadata.
- **JSON is not a round-trip contract:** `Result<T>` supports `System.Text.Json`
  serialization, but not deserialization (it has multiple public constructors).
  `StatusCode` is omitted from JSON, so it cannot survive a round-trip. Do not use
  `Result<T>` as an input/deserialization model. Pagination result types may round-trip on
  the tested runtimes, but this is not a supported contract.
- **Value-type failures:** a failed `Result<int>` contains `Data == 0`, so failure and a
  legitimate zero payload are not distinguishable through `Data` alone.

## License

MIT © 2025 Mecesoft
