using Api.UseCases.Notifications.DTOs;

namespace Api.Web.Endpoints.NotificationConfigs;

public class ListNotificationConfigsSummary : Summary<ListNotificationConfigsEndpoint>
{
    public ListNotificationConfigsSummary()
    {
        Summary = "Lấy danh sách cấu hình thông báo";
        Description = "Trả về tất cả loại thông báo và role được nhận thông báo đó. Chỉ Admin.";
        Response<IEnumerable<NotificationConfigDto>>(200, "Danh sách cấu hình");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
    }
}
