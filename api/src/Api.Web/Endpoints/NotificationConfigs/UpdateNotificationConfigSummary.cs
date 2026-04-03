using Api.UseCases.Notifications.DTOs;

namespace Api.Web.Endpoints.NotificationConfigs;

public class UpdateNotificationConfigSummary : Summary<UpdateNotificationConfigEndpoint>
{
    public UpdateNotificationConfigSummary()
    {
        Summary = "Cập nhật cấu hình thông báo";
        Description = "Thay đổi danh sách role nhận thông báo và bật/tắt loại thông báo. Chỉ Admin.";
        ExampleRequest = new UpdateNotificationConfigRequest
        {
            Id = 1,
            TargetRoles = ["Staff", "Admin"],
            IsEnabled = true,
        };
        Response<NotificationConfigDto>(200, "Cập nhật thành công");
        Response(400, "Dữ liệu không hợp lệ");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
        Response(404, "Không tìm thấy cấu hình");
    }
}
