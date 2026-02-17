using Api.UseCases.Products.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public class UpdateProductRequest
{
  public int ProductId { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string? Description { get; set; }
  public string? ImageUrl { get; set; }
}

public class Update : Endpoint<UpdateProductRequest>
{
  private readonly IMediator _mediator;

  public Update(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Put("/api/products/{ProductId}");
    AllowAnonymous();
    Summary(s => s.Summary = "Cập nhật product");
  }

  public override async Task HandleAsync(UpdateProductRequest req, CancellationToken ct)
  {
    var result = await _mediator.Send(
      new UpdateProductCommand(req.ProductId, req.Name, req.Price, req.Description, req.ImageUrl), ct);

    await this.SendResultAsync(result, ct);
  }
}
