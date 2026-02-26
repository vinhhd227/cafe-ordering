using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.GetUser;

public record GetUserQuery(Guid UserId) : IQuery<Result<UserDto>>;
