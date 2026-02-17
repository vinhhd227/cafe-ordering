namespace Api.Core.Events;

/// <summary>
///   Event khi entity được restore từ soft delete
/// </summary>
public class EntityRestoredEvent(int entityId, string entityType) : DomainEventBase
{
  public int EntityId { get; } = entityId;
  public string EntityType { get; } = entityType;
}
