---
title: API Conventions — Backend
tags: [backend, fastendpoints, cqrs, result, specification, conventions]
updated: 2026-04-07
---

# API Conventions — Backend

Xem thêm: [[architecture]], [[domain-model]]

---

## FastEndpoints — Endpoint Pattern

Mỗi endpoint = 1 file, đặt trong `Api.Web/Endpoints/[Feature]/`.
Luôn tạo kèm `[EndpointName]Summary` class trong cùng thư mục.

```csharp
// CreateTable.cs
public class CreateTable(IMediator mediator) : Endpoint<CreateTableRequest, TableDto>
{
    public override void Configure()
    {
        Post("/api/admin/tables");
        Policies("table.create");   // policy-based authorization
        DontAutoTag();
        Description(b => b.WithTags("Tables"));
    }

    public override async Task HandleAsync(CreateTableRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTableCommand(req.Code, req.ZoneId), ct);
        await this.SendResultAsync(result, ct);  // map Ardalis.Result → HTTP
    }
}

// CreateTableSummary.cs
public class CreateTableSummary : Summary<CreateTable>
{
    public CreateTableSummary()
    {
        Summary = "Tạo bàn mới";
        Description = "Tạo một bàn mới trong hệ thống";
        Response<TableDto>(200, "Tạo thành công");
        Response(400, "Dữ liệu không hợp lệ");
        Response(401, "Chưa đăng nhập");
        Response(403, "Không có quyền");
    }
}
```

### Endpoint inheritance

| Loại | Kế thừa |
|------|---------|
| Có request + response | `Endpoint<TRequest, TResponse>` |
| Có request, không response | `Ep.Req<TRequest>.NoRes` |
| Không request, có response | `EndpointWithoutRequest<TResponse>` |
| Không request, không response | `EndpointWithoutRequest` |

> **Quan trọng:** Endpoint dùng `Ep.Req<T>.NoRes` (bind từ route) **vẫn yêu cầu** `Content-Type: application/json` khi client gọi PUT/POST. Frontend phải gửi `{}` body — nếu không backend trả `415 Unsupported Media Type`.

---

## CQRS (Mediator source generator)

```csharp
// Command (có side effect)
public record CreateTableCommand(string Code, int? ZoneId) : ICommand<Result<TableDto>>;

// Query (chỉ đọc)
public record ListTablesQuery(int Page, int PageSize) : IQuery<Result<TableListDto>>;

// Handler
public class CreateTableHandler(IRepositoryBase<Table> repo)
    : ICommandHandler<CreateTableCommand, Result<TableDto>>
{
    public async ValueTask<Result<TableDto>> Handle(CreateTableCommand cmd, CancellationToken ct)
    {
        var existing = await repo.FirstOrDefaultAsync(new TableByCodeSpec(cmd.Code), ct);
        if (existing is not null)
            return Result.Invalid(new ValidationError("Code", "Mã bàn đã tồn tại"));

        var table = Table.Create(cmd.Code, cmd.ZoneId);
        await repo.AddAsync(table, ct);
        return Result.Success(new TableDto(table));
    }
}
```

Handler phải implement `ICommandHandler<TCommand, TResult>` hoặc `IQueryHandler<TQuery, TResult>`. Source generator tự tạo DI registration.

---

## Ardalis.Result Pattern

```csharp
// Trả về
return Result.Success(dto);
return Result.NotFound();
return Result.Invalid(new ValidationError("field", "message"));
return Result.Error("something went wrong");
return Result.Unauthorized();
return Result.Forbidden();

// Map sang HTTP (trong Endpoint):
await this.SendResultAsync(result, ct);  // dùng ResultExtensions.cs
```

**HTTP mapping:**

| Result | HTTP Status |
|--------|------------|
| `Result.Success` | `200 OK` |
| `Result.NoContent` | `204 No Content` |
| `Result.NotFound` | `404 Not Found` |
| `Result.Invalid` | `400 Bad Request` |
| `Result.Unauthorized` | `401 Unauthorized` |
| `Result.Forbidden` | `403 Forbidden` |
| `Result.Error` | `500 Internal Server Error` |

---

## Specification Pattern (Ardalis.Specification)

Đặt trong `Aggregates/[Feature]Aggregate/Specifications/`.

```csharp
// SingleResultSpecification — dùng cho FirstOrDefault
public class TableByCodeSpec : SingleResultSpecification<Table>
{
    public TableByCodeSpec(string code) =>
        Query.Where(t => t.Code == code && !t.IsDeleted);
}

// Specification — dùng cho List
public class AllTablesSpec : Specification<Table, TableDto>
{
    public AllTablesSpec(int page, int pageSize) =>
        Query
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Code)
            .Include(t => t.Zone)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TableDto(t.Id, t.Code, t.Status, t.Zone?.Name));
}
```

**Repository injection:**
```csharp
IRepositoryBase<Table> repo   // đọc + ghi
IReadRepositoryBase<Table> repo // chỉ đọc (AsNoTracking)
```

---

## Domain Entities — Conventions

```csharp
// 1. Private constructor
private Table() { }

// 2. Private setters
public string Code { get; private set; }

// 3. Static factory method
public static Table Create(string code, int? zoneId = null)
{
    var table = new Table
    {
        Code   = Guard.Against.NullOrWhiteSpace(code),
        ZoneId = zoneId,
        QrToken = Guid.NewGuid()
    };
    return table;
}

// 4. Behavior methods thay vì set trực tiếp
public void Activate() { IsActive = true; }
public void OpenSession(Guid sessionId) {
    Status = TableStatus.Occupied;
    RegisterDomainEvent(new TableSessionOpenedEvent(Id, sessionId));
}
```

---

## Authorization

- **Policy-based**: tên policy = tên claim trong JWT
- Gán policy trong `Configure()`: `Policies("table.create")`
- Claim được seed vào `identity.RoleClaims` theo role khi startup
- Ví dụ claims: `table.create`, `table.read`, `product.update`, `user.deactivate`

Shorthand policies:
```csharp
Policy("AdminOnly")      // chỉ Admin role
Policy("StaffOrAdmin")   // Admin hoặc Staff
Policy("table.create")   // permission claim cụ thể
```

---

## Guard Clauses (Ardalis.GuardClauses)

```csharp
Guard.Against.NullOrWhiteSpace(code, nameof(code));
Guard.Against.NullOrEmpty(orderNumber, nameof(orderNumber));
Guard.Against.Default(sessionId, nameof(sessionId));   // Guid.Empty check
Guard.Against.NegativeOrZero(tableId, nameof(tableId));
```

---

## Hai DbContext

| Context | Schema | Dùng khi |
|---------|--------|---------|
| `AppDbContext` | `business` | Repository cho domain entities |
| `AppIdentityDbContext` | `identity` | `UserManager`, `RoleManager`, tokens |

Injection trong handler:
```csharp
// Business entities → dùng IRepositoryBase<T>
IRepositoryBase<Table> tableRepo

// Identity → inject trực tiếp
UserManager<AppUser> userManager
```

---

## ValidationBehavior

Pipeline behavior tự động validate request trước khi handler chạy. Validator dùng `FluentValidation`:

```csharp
public class CreateTableRequestValidator : AbstractValidator<CreateTableRequest>
{
    public CreateTableRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
    }
}
```

Nếu validation fail → `Result.Invalid(...)` trả về tự động, không vào handler.
