using Api.UseCases.Notifications.DTOs;
using Api.UseCases.NotificationConfigs.GetSettings;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.NotificationConfigs;

public class GetNotificationSettingsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<NotificationSettingsDto>
{
    public override void Configure()
    {
        Get("/api/admin/notification-settings");
        Policies("AdminOnly");
        DontAutoTag();
        Description(b => b.WithTags("NotificationConfigs"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetNotificationSettingsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}

public class GetNotificationSettingsSummary : Summary<GetNotificationSettingsEndpoint>
{
    public GetNotificationSettingsSummary()
    {
        Summary = "Lấy cài đặt hệ thống thông báo";
        Description = "Trả về cài đặt toàn cục cho hệ thống thông báo (ví dụ: số ngày giữ thông báo).";
        Response<NotificationSettingsDto>(200, "Thành công");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền Admin");
        Response(404, "Chưa có bản ghi settings (chưa seed)");
    }
}
