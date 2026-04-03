using FastEndpoints;

namespace Api.Web.Endpoints.Push;

public class GetVapidPublicKeySummary : Summary<GetVapidPublicKeyEndpoint>
{
    public GetVapidPublicKeySummary()
    {
        Summary = "Lấy VAPID public key";
        Description = "Trả về VAPID public key để frontend dùng khi subscribe push notifications.";
        Response<string>(200, "Public key (base64url)");
        Response(401, "Chưa xác thực");
        Response(403, "Không có quyền");
    }
}
