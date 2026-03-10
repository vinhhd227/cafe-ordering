namespace Api.UseCases.Expenses.DTOs;

public record ExpenseByCategoryDto(
  decimal Ingredient,
  decimal Supply,
  decimal Equipment,
  decimal Other,
  decimal Total,
  decimal Cash,
  decimal Bank
);

public record RevenueSummaryDto(
  decimal Cash,
  decimal Bank,
  decimal Total
);

public record ExpenseSummaryDto(
  RevenueSummaryDto Revenue,
  ExpenseByCategoryDto Expenses,
  decimal Profit
);
