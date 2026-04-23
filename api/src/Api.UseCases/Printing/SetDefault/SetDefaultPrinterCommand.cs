namespace Api.UseCases.Printing.SetDefault;

public record SetDefaultPrinterCommand(int Id) : ICommand<Result>;
