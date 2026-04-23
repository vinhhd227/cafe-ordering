namespace Api.UseCases.Printing.Delete;

public record DeletePrinterConfigCommand(int Id, string DeletedBy) : ICommand<Result>;
