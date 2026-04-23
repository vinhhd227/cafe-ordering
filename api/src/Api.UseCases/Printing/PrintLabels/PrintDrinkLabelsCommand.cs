using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.PrintLabels;

public record PrintDrinkLabelsCommand(
  int    OrderId,
  int[]? ItemIds,
  int?   PrinterId,
  string? Role,
  int    CopiesPerItem = 1
) : ICommand<Result<PrintLabelsResultDto>>;
