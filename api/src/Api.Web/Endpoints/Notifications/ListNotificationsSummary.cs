using Api.UseCases.Notifications.DTOs;

namespace Api.Web.Endpoints.Notifications;

public class ListNotificationsSummary : Summary<ListNotificationsEndpoint>
{
    public ListNotificationsSummary()
    {
        Summary = "Lấy danh sách thông báo của user hiện tại";
        Description = "Trả về danh sách thông báo phân trang, tổng số và số chưa đọc.";
        Response<NotificationListDto>(200, "Danh sách thông báo");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
    }
}
