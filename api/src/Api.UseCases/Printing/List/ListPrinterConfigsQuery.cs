using Api.UseCases.Printing.DTOs;

namespace Api.UseCases.Printing.List;

public record ListPrinterConfigsQuery : IQuery<Result<List<PrinterConfigDto>>>;
