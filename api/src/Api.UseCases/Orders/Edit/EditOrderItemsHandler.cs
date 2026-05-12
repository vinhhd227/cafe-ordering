using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;

namespace Api.UseCases.Orders.Edit;

public class EditOrderItemsHandler(
  IRepositoryBase<Order> repository,
  IReadRepositoryBase<Product> productRepository)
  : ICommandHandler<EditOrderItemsCommand, Result>
{
  public async ValueTask<Result> Handle(EditOrderItemsCommand request, CancellationToken ct)
  {
    var order = await repository.FirstOrDefaultAsync(
      new OrderByIdWithItemsSpec(request.OrderId), ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    if (!order.Status.CanAddItems)
      return Result.Invalid(new ValidationError("Status", "Only Pending orders can be edited."));

    if (order.PaymentStatus != PaymentStatus.Unpaid)
      return Result.Invalid(new ValidationError("PaymentStatus", "Cannot edit an already paid order."));

    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsWithAttributesSpec(productIds), ct);

    if (products.Count != productIds.Count)
    {
      var missing = productIds.Except(products.Select(p => p.Id)).First();
      return Result.NotFound($"Product {missing} not found.");
    }

    var productMap = products.ToDictionary(p => p.Id);

    order.ClearAllItems();

    foreach (var item in request.Items)
    {
      var product = productMap[item.ProductId];

      var optionData = BuildOptionData(product, item.SelectedOptionValueIds);
      if (optionData is null)
        return Result.Invalid(new ValidationError("SelectedOptionValueIds",
          $"Invalid option value IDs for product '{product.Name}'."));

      order.AddItem(item.ProductId, product.Name, product.Price, item.Quantity,
        optionData, item.IsTakeaway, isFreeGift: false, item.Note);
    }

    order.UpdateGuestCount(request.GuestCount);

    await repository.UpdateAsync(order, ct);

    return Result.Success();
  }

  private static IReadOnlyList<OrderItemOptionData>? BuildOptionData(
    Product product, List<int>? selectedValueIds)
  {
    if (selectedValueIds is null || selectedValueIds.Count == 0)
      return [];

    var result = new List<OrderItemOptionData>();

    foreach (var valueId in selectedValueIds)
    {
      var group = product.AttributeGroups
        .FirstOrDefault(g => g.Values.Any(v => v.Id == valueId));

      if (group is null) return null;

      var value = group.Values.First(v => v.Id == valueId);
      result.Add(new OrderItemOptionData(value.Id, group.Name, value.Label, value.PriceAdjustment));
    }

    return result;
  }
}
