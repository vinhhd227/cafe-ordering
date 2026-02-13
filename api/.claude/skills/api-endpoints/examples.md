# ApiEndpoints Examples

Quick reference examples for common patterns.

## Complete CRUD Endpoints

```csharp
// Features/Customers/Create/Endpoint.cs
public class Create : EndpointBaseAsync
    .WithRequest<Request>
    .WithActionResult<Response>
{
    private readonly IRepository<Customer> _repo;

    public Create(IRepository<Customer> repo) => _repo = repo;

    [HttpPost("/api/customers")]
    public override async Task<ActionResult<Response>> HandleAsync(
        Request req, CancellationToken ct = default)
    {
        var customer = new Customer(req.FirstName, req.LastName, req.Email);
        await _repo.AddAsync(customer, ct);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, 
            new Response { Id = customer.Id });
    }
}

public record Request(string FirstName, string LastName, string Email);
public record Response { public int Id { get; init; } }

// Features/Customers/Get/Endpoint.cs
public class Get : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<Response>
{
    [HttpGet("/api/customers/{id}")]
    public override async Task<ActionResult<Response>> HandleAsync(
        [FromRoute] int id, CancellationToken ct = default)
    {
        var customer = await _repo.GetByIdAsync(id, ct);
        return customer == null ? NotFound() : Ok(Map(customer));
    }
}

// Features/Customers/Update/Endpoint.cs  
public class Update : EndpointBaseAsync
    .WithRequest<Request>
    .WithActionResult<Response>
{
    [HttpPut("/api/customers/{id}")]
    public override async Task<ActionResult<Response>> HandleAsync(
        [FromRoute] Request req, CancellationToken ct = default)
    {
        var customer = await _repo.GetByIdAsync(req.Id, ct);
        if (customer == null) return NotFound();
        
        customer.Update(req.FirstName, req.LastName);
        await _repo.UpdateAsync(customer, ct);
        return Ok(Map(customer));
    }
}

public record Request([FromRoute] int Id, [FromBody] string FirstName, [FromBody] string LastName);

// Features/Customers/Delete/Endpoint.cs
public class Delete : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult
{
    [HttpDelete("/api/customers/{id}")]
    public override async Task<ActionResult> HandleAsync(
        [FromRoute] int id, CancellationToken ct = default)
    {
        var customer = await _repo.GetByIdAsync(id, ct);
        if (customer == null) return NotFound();
        
        await _repo.DeleteAsync(customer, ct);
        return NoContent();
    }
}
```

See SKILL.md for complete guide. **For new projects, use FastEndpoints!**
