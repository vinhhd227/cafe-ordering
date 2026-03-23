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
    // 1. Load order với items
    var order = await repository.FirstOrDefaultAsync(
      new OrderByIdWithItemsSpec(request.OrderId), ct);

    if (order is null)
      return Result.NotFound($"Order {request.OrderId} not found.");

    // 2. Validate: chỉ sửa được order PENDING + UNPAID
    if (!order.Status.CanAddItems)
      return Result.Invalid(new ValidationError("Status", "Only Pending orders can be edited."));

    if (order.PaymentStatus != PaymentStatus.Unpaid)
      return Result.Invalid(new ValidationError("PaymentStatus", "Cannot edit an already paid order."));

    // 3. Validate: phải có ít nhất 1 item
    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    // 4. Load tất cả products để validate và lấy giá hiện tại
    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsSpec(productIds), ct);

    if (products.Count != productIds.Count)
    {
      var missing = productIds.Except(products.Select(p => p.Id)).First();
      return Result.NotFound($"Product {missing} not found.");
    }

    var productMap = products.ToDictionary(p => p.Id);

    // 5. Xoá toàn bộ items + promotions cũ
    order.ClearAllItems();

    // 6. Re-add từng item với giá hiện tại từ DB
    foreach (var item in request.Items)
    {
      var product = productMap[item.ProductId];

      DrinkTemperature? temp = item.Temperature is not null
        ? DrinkTemperature.FromName(item.Temperature.Trim().ToUpperInvariant(), true) : null;
      IceLevel? iceLevel = item.IceLevel is not null
        ? IceLevel.FromName(item.IceLevel.Trim().ToUpperInvariant(), true) : null;
      SugarLevel? sugarLevel = item.SugarLevel is not null
        ? SugarLevel.FromName(item.SugarLevel.Trim().ToUpperInvariant(), true) : null;

      order.AddItem(item.ProductId, product.Name, product.Price, item.Quantity,
        temp, iceLevel, sugarLevel, item.IsTakeaway, isFreeGift: false, item.Note);
    }

    // 7. Cập nhật số khách
    order.UpdateGuestCount(request.GuestCount);

    // 8. Persist
    await repository.UpdateAsync(order, ct);

    return Result.Success();
  }
}
