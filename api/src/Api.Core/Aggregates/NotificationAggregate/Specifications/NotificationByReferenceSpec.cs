namespace Api.Core.Aggregates.NotificationAggregate.Specifications;

/// <summary>
/// Checks whether a notification for a given type + referenceId already exists.
/// Used for deduplication in NotificationService to prevent duplicate sends
/// when domain events fire multiple times (e.g. multiple SaveChanges in one request).
/// NOTE: must compare NotificationType via SmartEnum equality (not .Name),
/// because EF value converter stores as string — .Name on the property is not translatable.
/// </summary>
public class NotificationByReferenceSpec : Specification<Notification>
{
    public NotificationByReferenceSpec(NotificationType type, int referenceId)
    {
        Query.Where(n => n.Type == type && n.ReferenceId == referenceId);
    }
}
