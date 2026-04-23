namespace Api.UseCases.Printing.TestConnection;

public record TestPrinterConnectionCommand(int PrinterId) : ICommand<Result<bool>>;
