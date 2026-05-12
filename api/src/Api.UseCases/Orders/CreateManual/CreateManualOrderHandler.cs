using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.CreateManual;

public class CreateManualOrderHandler(
  IRepositoryBase<Order> orderRepository,
  IRepositoryBase<GuestSession> sessionRepository,
  IReadRepositoryBase<Table> tableRepository,
  IReadRepositoryBase<Product> productRepository)
  : ICommandHandler<CreateManualOrderCommand, Result<OrderDto>>
{
  public async ValueTask<Result<OrderDto>> Handle(CreateManualOrderCommand request, CancellationToken ct)
  {
    var table = await tableRepository.FirstOrDefaultAsync(new TableByIdSpec(request.TableId), ct);
    if (table is null)
      return Result.NotFound($"Table {request.TableId} not found.");

    if (request.Items is null || request.Items.Count == 0)
      return Result.Invalid(new ValidationError("Items", "Order must contain at least one item."));

    if (!OrderStatus.TryFromName(request.Status, true, out var status))
      return Result.Invalid(new ValidationError("Status", $"Invalid status: {request.Status}"));

    if (!PaymentStatus.TryFromName(request.PaymentStatus, true, out var paymentStatus))
      return Result.Invalid(new ValidationError("PaymentStatus", $"Invalid payment status: {request.PaymentStatus}"));

    if (!PaymentMethod.TryFromName(request.PaymentMethod, true, out var paymentMethod))
      return Result.Invalid(new ValidationError("PaymentMethod", $"Invalid payment method: {request.PaymentMethod}"));

    var existingSession = await sessionRepository.FirstOrDefaultAsync(
      new ActiveSessionByTableIdSpec(request.TableId), ct);

    Guid sessionId;
    if (existingSession is not null)
    {
      sessionId = existingSession.Id;
    }
    else
    {
      var session = GuestSession.CreateManual(request.TableId);
      await sessionRepository.AddAsync(session, ct);
      sessionId = session.Id;
    }

    var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";
    var order = Order.Create(sessionId, orderNumber, guestCount: request.GuestCount, orderedAt: request.OrderedAt);
    await orderRepository.AddAsync(order, ct);

    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await productRepository.ListAsync(new ProductsByIdsWithAttributesSpec(productIds), ct);

    if (products.Count != productIds.Count)
    {
      var missing = productIds.Except(products.Select(p => p.Id)).First();
      return Result.NotFound($"Product {missing} not found.");
    }

    var productMap = products.ToDictionary(p => p.Id);

    foreach (var item in request.Items)
    {
      var product = productMap[item.ProductId];

      var optionData = BuildOptionData(product, item.SelectedOptionValueIds);
      if (optionData is null)
        return Result.Invalid(new ValidationError("SelectedOptionValueIds",
          $"Invalid option value IDs for product '{product.Name}'."));

      order.AddItemManual(item.ProductId, product.Name, product.Price, item.Quantity,
        optionData, item.IsTakeaway, item.Note);
    }

    if (status != OrderStatus.Pending)
      order.ForceSetStatus(status);

    if (paymentStatus != PaymentStatus.Unpaid || paymentMethod != PaymentMethod.Unknown
        || request.AmountReceived.HasValue || request.TipAmount != 0)
    {
      order.UpdatePayment(paymentStatus, paymentMethod, request.AmountReceived, request.TipAmount);
    }

    await orderRepository.UpdateAsync(order, ct);

    return Result.Success(order.ToOrderDto(table.Code, isManual: true));
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
