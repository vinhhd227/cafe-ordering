namespace Api.Core.Aggregates.NotificationAggregate;

public class NotificationConfig : AuditableEntity, IAggregateRoot
{
    public string Type { get; private set; } = default!;
    public List<string> TargetRoles { get; private set; } = [];
    public bool IsEnabled { get; private set; } = true;

    private NotificationConfig() { }

    public static NotificationConfig Create(string type, List<string> targetRoles, bool isEnabled = true)
        => new() { Type = type, TargetRoles = targetRoles, IsEnabled = isEnabled };

    public void Update(List<string> targetRoles, bool isEnabled)
    {
        TargetRoles = targetRoles;
        IsEnabled = isEnabled;
    }
}
