using Api.UseCases.Notifications.DTOs;
using Api.UseCases.NotificationConfigs.UpdateSettings;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.NotificationConfigs;

public class UpdateNotificationSettingsEndpoint(IMediator mediator)
    : Endpoint<UpdateNotificationSettingsRequest, NotificationSettingsDto>
{
    public override void Configure()
    {
        Put("/api/admin/notification-settings");
        Policies("admin.access", "notification.config");
        DontAutoTag();
        Description(b => b.WithTags("NotificationConfigs"));
    }

    public override async Task HandleAsync(UpdateNotificationSettingsRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateNotificationSettingsCommand(req.RetentionDays), ct);
        await this.SendResultAsync(result, ct);
    }
}

public class UpdateNotificationSettingsRequest
{
    public int RetentionDays { get; set; }
}

public class UpdateNotificationSettingsSummary : Summary<UpdateNotificationSettingsEndpoint>
{
    public UpdateNotificationSettingsSummary()
    {
        Summary = "Cập nhật cài đặt hệ thống thông báo";
        Description = "Cập nhật cài đặt toàn cục cho hệ thống thông báo. RetentionDays: số ngày giữ thông báo trước khi xóa tự động (1–365).";
        ExampleRequest = new UpdateNotificationSettingsRequest { RetentionDays = 30 };
        Response<NotificationSettingsDto>(200, "Cập nhật thành công");
        Response(400, "RetentionDays nằm ngoài khoảng hợp lệ (1–365)");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền Admin");
        Response(404, "Chưa có bản ghi settings (chưa seed)");
    }
}
