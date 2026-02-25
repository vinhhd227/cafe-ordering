using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate.Specifications;
using Api.UseCases.Orders.DTOs;

namespace Api.UseCases.Orders.List;

public class ListOrdersHandler(IReadRepositoryBase<Order> repository)
  : IQueryHandler<ListOrdersQuery, Result<List<OrderDto>>>
{
  public async ValueTask<Result<List<OrderDto>>> Handle(
    ListOrdersQuery request, CancellationToken ct)
  {
    var spec = new OrdersListSpec(request.Status);
    var orders = await repository.ListAsync(spec, ct);

    var dtos = orders.Select(o => new OrderDto(
      o.Id,
      o.OrderNumber,
      o.Status.Name,
      o.TotalAmount,
      o.OrderDate,
      o.SessionId,
      o.Items.Select(i => new OrderItemDto(
        i.ProductId,
        i.ProductName,
        i.UnitPrice,
        i.Quantity,
        i.TotalPrice
      )).ToList()
    )).ToList();

    return Result.Success(dtos);
  }
}
