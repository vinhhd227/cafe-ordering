namespace Api.UseCases.Expenses.DTOs;

public record ExpenseByCategoryDto(
  long Ingredient,
  long Supply,
  long Equipment,
  long Other,
  long Total,
  long Cash,
  long Bank
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
