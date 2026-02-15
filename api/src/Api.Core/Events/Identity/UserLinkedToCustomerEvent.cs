namespace Api.Core.Events.Identity;

/// <summary>
/// Domain event raised when a user is linked to a customer aggregate
/// </summary>
public class UserLinkedToCustomerEvent(int userId, string customerId) : DomainEventBase
{
  public int UserId { get; } = userId;
  public string CustomerId { get; } = customerId;
}
