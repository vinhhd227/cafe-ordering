using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.ResetUserPassword;

public record ResetUserPasswordCommand(Guid UserId) : ICommand<Result<TemporaryPasswordDto>>;
