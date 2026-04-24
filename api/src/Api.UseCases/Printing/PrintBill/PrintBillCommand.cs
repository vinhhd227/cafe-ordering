using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.PrintBill;

public record PrintBillCommand(
  int  OrderId,
  int? PrinterId
) : ICommand<Result<PrintLabelsResultDto>>;
