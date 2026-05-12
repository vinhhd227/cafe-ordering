using System.Security.Claims;
using Api.UseCases.ProductOptionGroups.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.ProductOptionGroups;

public class DeleteProductOptionGroupRequest { public int Id { get; set; } }

public class Delete(IMediator mediator) : Ep.Req<DeleteProductOptionGroupRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/product-option-groups/{Id}");
    Policies("product.delete");
    DontAutoTag();
    Description(b => b.WithTags("ProductOptionGroups"));
  }

  public override async Task HandleAsync(DeleteProductOptionGroupRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";
    var result = await mediator.Send(new DeleteProductOptionGroupCommand(req.Id, deletedBy), ct);
    await this.SendResultAsync(result, ct);
  }
}
