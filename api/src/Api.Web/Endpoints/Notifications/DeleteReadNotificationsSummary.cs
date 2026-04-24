namespace Api.Web.Endpoints.Notifications;

public class DeleteReadNotificationsSummary : Summary<DeleteReadNotificationsEndpoint>
{
    public DeleteReadNotificationsSummary()
    {
        Summary = "Xoá tất cả thông báo đã đọc";
        Description = "Xoá toàn bộ thông báo đã đọc của người dùng hiện tại. Trả về số lượng đã xoá.";
        Response<int>(200, "Xoá thành công, trả về số lượng đã xoá");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
    }
}
