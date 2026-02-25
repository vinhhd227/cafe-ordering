namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Basic identity information for an authenticated user.
/// </summary>
public sealed class UserDto
{
  public Guid Id { get; init; }
  public string? Username { get; init; }
  public string? Email { get; init; }
  public string? FullName { get; init; }
}
