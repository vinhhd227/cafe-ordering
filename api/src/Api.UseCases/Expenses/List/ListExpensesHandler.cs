using Api.Core.Aggregates.ExpenseAggregate;
using Api.Core.Aggregates.ExpenseAggregate.Specifications;
using Api.UseCases.Expenses.Create;
using Api.UseCases.Expenses.DTOs;

namespace Api.UseCases.Expenses.List;

public class ListExpensesHandler(IReadRepositoryBase<Expense> repository)
  : IQueryHandler<ListExpensesQuery, Result<PagedExpensesDto>>
{
  public async ValueTask<Result<PagedExpensesDto>> Handle(ListExpensesQuery request, CancellationToken ct)
  {
    // ToUniversalTime() xử lý đúng khi server chạy ở timezone Vietnam:
    // ASP.NET model binding parse "2026-03-09T17:00:00Z" → Local (March 10 00:00 VN),
    // ToUniversalTime() convert lại về March 9 17:00 UTC đúng.
    // (SpecifyKind không convert, chỉ đổi label → sai 7 tiếng.)
    var dateFrom = request.DateFrom.HasValue
      ? request.DateFrom.Value.ToUniversalTime()
      : (DateTime?)null;
    var dateTo = request.DateTo.HasValue
      ? request.DateTo.Value.ToUniversalTime()
      : (DateTime?)null;

    var countSpec = new ExpensesCountSpec(request.Category, request.PaymentMethod, dateFrom, dateTo);
    var listSpec  = new ExpensesListSpec(request.Category, request.PaymentMethod, dateFrom, dateTo, request.Page, request.PageSize);

    var totalCount = await repository.CountAsync(countSpec, ct);
    var expenses   = await repository.ListAsync(listSpec, ct);

    var dtos = expenses.Select(CreateExpenseHandler.ToDto).ToList();
    var grandTotal = dtos.Sum(d => d.TotalAmount);

    return Result.Success(new PagedExpensesDto(dtos, totalCount, request.Page, request.PageSize, grandTotal));
  }
}
