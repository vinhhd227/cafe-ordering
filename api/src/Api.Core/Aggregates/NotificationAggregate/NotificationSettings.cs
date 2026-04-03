using Ardalis.GuardClauses;

namespace Api.Core.Aggregates.NotificationAggregate;

/// <summary>
/// Single-row global settings for the notification subsystem (Id = 1 always).
/// </summary>
public class NotificationSettings : AuditableEntity, IAggregateRoot
{
    public const int DefaultRetentionDays = 30;
    public const int MinRetentionDays = 1;
    public const int MaxRetentionDays = 365;

    public int RetentionDays { get; private set; } = DefaultRetentionDays;

    private NotificationSettings() { }

    public static NotificationSettings Create(int retentionDays = DefaultRetentionDays)
    {
        Guard.Against.OutOfRange(retentionDays, nameof(retentionDays), MinRetentionDays, MaxRetentionDays);
        return new NotificationSettings { RetentionDays = retentionDays };
    }

    public void Update(int retentionDays)
    {
        Guard.Against.OutOfRange(retentionDays, nameof(retentionDays), MinRetentionDays, MaxRetentionDays);
        RetentionDays = retentionDays;
    }
}
