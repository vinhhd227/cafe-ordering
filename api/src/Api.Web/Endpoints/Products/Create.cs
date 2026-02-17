using Api.UseCases.Products.Create;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Products;

public class CreateProductRequest
{
  public int CategoryId { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public string? Description { get; set; }
  public string? ImageUrl { get; set; }
  public bool HasTemperatureOption { get; set; }
  public bool HasIceLevelOption { get; set; }
  public bool HasSugarLevelOption { get; set; }
}

public class Create : Endpoint<CreateProductRequest>
{
  private readonly IMediator _mediator;

  public Create(IMediator mediator)
  {
    _mediator = mediator;
  }

  public override void Configure()
  {
    Post("/api/products");
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Tạo product mới";
      s.Description = "Tạo mới một sản phẩm trong danh mục";
    });
  }

  public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
  {
    var command = new CreateProductCommand(
      req.CategoryId,
      req.Name,
      req.Price,
      req.Description,
      req.ImageUrl,
      req.HasTemperatureOption,
      req.HasIceLevelOption,
      req.HasSugarLevelOption);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      await SendAsync(new { Id = result.Value }, StatusCodes.Status201Created, ct);
      return;
    }

    await this.SendResultAsync(result, ct);
  }
}
