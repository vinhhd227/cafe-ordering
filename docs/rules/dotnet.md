# Backend Rules — .NET / ASP.NET Core

## Endpoint (FastEndpoints)

- Mỗi endpoint = 1 file, đặt trong `Api.Web/Endpoints/[Feature]/`
- **Luôn tạo `Summary` class kèm theo** trong cùng folder:
  - Tên: `{EndpointName}Summary`
  - Kế thừa: `Summary<{EndpointName}Endpoint>`
  - Bắt buộc có: `Summary`, `Description`, `ResponseExamples` cho 200, tất cả Response codes có thể xảy ra

```csharp
public class CreateTableSummary : Summary<CreateTableEndpoint>
{
    public CreateTableSummary()
    {
        Summary = "Tạo bàn mới";
        Description = "Tạo một bàn mới trong hệ thống.";
        Response<TableDto>(200, "Tạo thành công");
        Response(400, "Dữ liệu không hợp lệ");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
    }
}
```

- Endpoint **không có body**: kế thừa `Ep.Req<TRequest>.NoRes` — **bắt buộc gửi `{}` body khi gọi từ frontend**
- Endpoint có response: `Endpoint<TRequest, TResponse>`
- Endpoint không cần request: `EndpointWithoutRequest<TResponse>`
- Authorization: `Policies("feature.action")` — tên policy = tên claim

```csharp
public override void Configure()
{
    Post("/api/admin/tables");
    Policies("table.create");
    DontAutoTag();
    Description(b => b.WithTags("Tables"));
}
```

## CQRS (Mediator source generator)

Cấu trúc file trong `Api.UseCases/[Feature]/[Action]/`:
- `[Action][Feature]Command.cs` — record Command implement `ICommand<Result<TDto>>`
- `[Action][Feature]Handler.cs` — class Handler implement `ICommandHandler<TCommand, TResult>`
- Query tương tự: `IQuery<T>` / `IQueryHandler<TQuery, T>`

```csharp
// Command
public record CreateTableCommand(int Number, string Code) : ICommand<Result<TableDto>>;

// Handler
public class CreateTableHandler(IRepositoryBase<Table> repo)
    : ICommandHandler<CreateTableCommand, Result<TableDto>>
{
    public async ValueTask<Result<TableDto>> Handle(CreateTableCommand cmd, CancellationToken ct)
    {
        // ...
    }
}
```

## Domain Entities

- **Không dùng `new`** — luôn dùng static factory `Entity.Create(...)`
- **Setter private** cho tất cả properties
- Thay đổi state qua **behavior methods** (`Activate()`, `UpdateCode()`, ...)
- Soft delete: dùng `Delete()` / `Restore()` — không xóa vật lý
- Domain events: `RegisterDomainEvent(new SomeEvent(...))`
- Hierarchy: `BaseEntity<TId>` → `AuditableEntity<TId>` → `SoftDeletableEntity<TId>`

```csharp
public class Table : SoftDeletableEntity<int>, IAggregateRoot
{
    public int Number { get; private set; }

    public static Table Create(int number, string code) { ... }
    public void Activate() { IsActive = true; }
}
```

## Result Pattern (Ardalis.Result)

| Result | HTTP Status |
|--------|-------------|
| `Result.Success(dto)` | 200 OK |
| `Result.Success()` / `.NoRes` | 204 No Content |
| `Result.NotFound()` | 404 Not Found |
| `Result.Invalid(...)` | 400 Bad Request |
| `Result.Unauthorized()` | 401 Unauthorized |
| `Result.Forbidden()` | 403 Forbidden |
| `Result.Error(...)` | 500 Internal Server Error |

Dùng `await this.SendResultAsync(result, ct)` trong endpoint — tự động map sang HTTP status code.

## Specification Pattern

- File đặt trong: `Api.Core/Aggregates/[Feature]Aggregate/Specifications/`
- Naming: `[Entity]By[Field]Spec`, `[Entity]ListSpec`, `[Entity]By[Condition]Spec`

```csharp
public class TableByNumberSpec : SingleResultSpecification<Table>
{
    public TableByNumberSpec(int number) => Query.Where(t => t.Number == number);
}
```

## Authorization

- Policy name = permission claim: `"table.create"`, `"product.read"`, `"user.deactivate"`
- Claims được seed từ DB khi startup
- Role-based: `Admin`, `Staff`

## DbContext

| Context | Dùng khi |
|---------|----------|
| `AppDbContext` | Repository cho business entities (Table, Product, Order, ...) |
| `AppIdentityDbContext` | `UserManager`, `RoleManager`, user/role/claims |

Hai context có migration riêng biệt:
- Business: `Data/Migrations/`
- Identity: `Identity/Migrations/`

## Content-Type với FastEndpoints 6

- PUT/POST endpoint (kể cả bind từ route) **yêu cầu** `Content-Type: application/json`
- Frontend **phải gửi `{}` body** cho các endpoint không có body thực sự
- Không có `{}` → backend trả `415 Unsupported Media Type`
