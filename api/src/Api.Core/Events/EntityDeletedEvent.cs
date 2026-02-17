namespace Api.Core.Events;

/// <summary>
///   Event khi entity bị soft delete
/// </summary>
public class EntityDeletedEvent(int entityId, string entityType, string deletedBy) : DomainEventBase
{
  public int EntityId { get; } = entityId;
  public string EntityType { get; } = entityType;
  public string DeletedBy { get; } = deletedBy;
}
