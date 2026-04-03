namespace Api.Core.Aggregates.NotificationAggregate;

public class Notification : AuditableEntity, IAggregateRoot
{
    public string UserId { get; private set; } = default!;
    public NotificationType Type { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public string? Url { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public int? ReferenceId { get; private set; }

    private Notification() { }

    public static Notification Create(
        string userId,
        NotificationType type,
        string title,
        string body,
        string? url = null,
        int? referenceId = null)
        => new()
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Url = url,
            ReferenceId = referenceId,
        };

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
