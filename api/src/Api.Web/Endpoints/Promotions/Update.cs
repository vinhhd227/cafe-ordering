using Api.UseCases.Promotions.DTOs;
using Api.UseCases.Promotions.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Promotions;

public sealed class UpdatePromotionRequest
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string? Code { get; set; }
  public string? Description { get; set; }
  public string DiscountType { get; set; } = "PERCENTAGE";
  public decimal DiscountValue { get; set; }
  public int? BuyQuantity { get; set; }
  public int? GetQuantity { get; set; }
  public string Scope { get; set; } = "ORDER";
  public List<int>? ApplicableProductIds { get; set; }
  public List<int>? ApplicableCategoryIds { get; set; }
  public string StackPolicy { get; set; } = "EXCLUSIVE";
  public decimal? MinOrderAmount { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime? EndDate { get; set; }
  public int? MaxUsage { get; set; }
}

public class UpdatePromotion(IMediator mediator) : Endpoint<UpdatePromotionRequest, PromotionDto>
{
  public override void Configure()
  {
    Put("/api/admin/promotions/{id}");
    Policies("Admin");
    DontAutoTag();
    Description(b => b.WithTags("Promotions"));
  }

  public override async Task HandleAsync(UpdatePromotionRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UpdatePromotionCommand(
      req.Id, req.Name, req.Code, req.Description,
      req.DiscountType, req.DiscountValue, req.BuyQuantity, req.GetQuantity,
      req.Scope, req.ApplicableProductIds, req.ApplicableCategoryIds,
      req.StackPolicy, req.MinOrderAmount, req.StartDate, req.EndDate, req.MaxUsage), ct);

    await this.SendResultAsync(result, ct);
  }
}
