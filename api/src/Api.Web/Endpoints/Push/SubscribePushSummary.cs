using FastEndpoints;

namespace Api.Web.Endpoints.Push;

public class SubscribePushSummary : Summary<SubscribePushEndpoint>
{
    public SubscribePushSummary()
    {
        Summary = "Đăng ký nhận web push notification";
        Description = "Lưu browser push subscription của user hiện tại. Nếu endpoint đã tồn tại thì cập nhật keys.";
        Response(204, "Đăng ký thành công");
        Response(400, "Dữ liệu không hợp lệ");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
    }
}
