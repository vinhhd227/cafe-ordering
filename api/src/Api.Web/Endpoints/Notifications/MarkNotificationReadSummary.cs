namespace Api.Web.Endpoints.Notifications;

public class MarkNotificationReadSummary : Summary<MarkNotificationReadEndpoint>
{
    public MarkNotificationReadSummary()
    {
        Summary = "Đánh dấu thông báo đã đọc";
        Description = "Đánh dấu một thông báo cụ thể là đã đọc.";
        Response(204, "Đánh dấu thành công");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
        Response(404, "Không tìm thấy thông báo");
    }
}
