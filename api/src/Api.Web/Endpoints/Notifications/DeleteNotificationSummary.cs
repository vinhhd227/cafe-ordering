namespace Api.Web.Endpoints.Notifications;

public class DeleteNotificationSummary : Summary<DeleteNotificationEndpoint>
{
    public DeleteNotificationSummary()
    {
        Summary = "Xoá thông báo";
        Description = "Xoá một thông báo của người dùng hiện tại.";
        Response(204, "Xoá thành công");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
        Response(404, "Không tìm thấy thông báo");
    }
}
