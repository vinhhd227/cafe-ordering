using FastEndpoints;

namespace Api.Web.Endpoints.Push;

public class UnsubscribePushSummary : Summary<UnsubscribePushEndpoint>
{
    public UnsubscribePushSummary()
    {
        Summary = "Hủy đăng ký web push notification";
        Description = "Xóa browser push subscription theo endpoint.";
        Response(204, "Hủy đăng ký thành công");
        Response(400, "Dữ liệu không hợp lệ");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
        Response(404, "Không tìm thấy subscription");
    }
}
