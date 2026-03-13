using System.Security.Claims;
using Api.UseCases.Promotions.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Promotions;

public sealed class DeletePromotionRequest
{
  public int Id { get; set; }
}

public class DeletePromotion(IMediator mediator) : Endpoint<DeletePromotionRequest>
{
  public override void Configure()
  {
    Delete("/api/admin/promotions/{id}");
    Policies("promotion.delete");
    DontAutoTag();
    Description(b => b.WithTags("Promotions"));
  }

  public override async Task HandleAsync(DeletePromotionRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";
    var result = await mediator.Send(new DeletePromotionCommand(req.Id, deletedBy), ct);
    await this.SendResultAsync(result, ct);
  }
}
