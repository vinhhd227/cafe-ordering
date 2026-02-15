namespace Api.Core.Events.Identity;

/// <summary>
/// Domain event raised when user email is confirmed
/// </summary>
public class UserEmailConfirmedEvent(int userId, string email) : DomainEventBase
{
  public int UserId { get; } = userId;
  public string Email { get; } = email;
}
