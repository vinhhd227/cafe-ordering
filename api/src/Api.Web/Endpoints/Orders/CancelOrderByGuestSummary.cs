namespace Api.Web.Endpoints.Orders;

public class CancelOrderByGuestSummary : Summary<CancelOrderByGuest>
{
  public CancelOrderByGuestSummary()
  {
    Summary = "Huỷ đơn hàng (guest)";
    Description = "Cho phép khách huỷ đơn Pending trong vòng 2 phút sau khi đặt.";
    Response(204, "Huỷ thành công");
    Response(400, "Ngoài cửa sổ thời gian hoặc trạng thái không hợp lệ");
    Response(403, "SessionId không khớp");
    Response(404, "Không tìm thấy đơn");
  }
}
