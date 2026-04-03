namespace Api.Web.Endpoints.Notifications;

public class GetUnreadCountSummary : Summary<GetUnreadCountEndpoint>
{
    public GetUnreadCountSummary()
    {
        Summary = "Lấy số thông báo chưa đọc";
        Description = "Trả về tổng số thông báo chưa đọc của user hiện tại.";
        Response<int>(200, "Số thông báo chưa đọc");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
    }
}
