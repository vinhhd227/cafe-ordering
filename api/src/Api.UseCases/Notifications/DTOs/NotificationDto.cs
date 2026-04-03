namespace Api.UseCases.Notifications.DTOs;

public record NotificationDto(
    int Id,
    string Type,
    string Title,
    string Body,
    string? Url,
    bool IsRead,
    DateTime? ReadAt,
    int? ReferenceId,
    DateTime CreatedAt);

public record NotificationListDto(
    IEnumerable<NotificationDto> Items,
    int TotalCount,
    int UnreadCount);

public record NotificationConfigDto(
    int Id,
    string Type,
    List<string> TargetRoles,
    bool IsEnabled);

public record NotificationSettingsDto(int RetentionDays);
