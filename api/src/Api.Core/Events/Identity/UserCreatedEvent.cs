using Api.Core.Entities.Identity;

namespace Api.Core.Events.Identity;

/// <summary>
/// Domain event raised when a new user is created
/// </summary>
public class UserCreatedEvent(ApplicationUser user) : DomainEventBase
{
  public int UserId { get; } = user.Id;
  public string UserName { get; } = user.UserName;
  public string Email { get; } = user.Email;
}
