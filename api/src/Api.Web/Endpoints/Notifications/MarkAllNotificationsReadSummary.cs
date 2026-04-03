namespace Api.Web.Endpoints.Notifications;

public class MarkAllNotificationsReadSummary : Summary<MarkAllNotificationsReadEndpoint>
{
    public MarkAllNotificationsReadSummary()
    {
        Summary = "Đánh dấu tất cả thông báo đã đọc";
        Description = "Đánh dấu tất cả thông báo chưa đọc của user hiện tại là đã đọc.";
        Response(204, "Đánh dấu thành công");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
    }
}
