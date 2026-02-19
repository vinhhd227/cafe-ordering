namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Basic identity information for an authenticated user.
/// </summary>
public sealed class UserDto
{
  /// <summary>The internal integer identifier of the user.</summary>
  public int Id { get; init; }

  /// <summary>The email address associated with the user account.</summary>
  public string? Email { get; init; }
}
