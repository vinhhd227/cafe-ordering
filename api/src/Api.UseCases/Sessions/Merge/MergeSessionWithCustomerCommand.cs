namespace Api.UseCases.Sessions.Merge;

public record MergeSessionWithCustomerCommand(Guid SessionId, string CustomerId) : ICommand<Result>;
