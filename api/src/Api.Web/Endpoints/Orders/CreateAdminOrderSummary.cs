using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class CreateAdminOrderSummary : Summary<CreateAdminOrder>
{
  public CreateAdminOrderSummary()
  {
    Summary = "Tao don hang moi (admin wizard)";
    Description = "Tao don hang moi ho tro ca 3 loai: Dine-in (can session), Takeaway, Delivery. " +
                  "Doi voi Dine-in phai truyen SessionId. Doi voi Delivery phai truyen DeliveryAddress.";
    Response<PlaceOrderResponseDto>(200, "Tao thanh cong");
    Response(400, "Du lieu khong hop le");
    Response(401, "Chua xac thuc");
    Response(403, "Khong co quyen");
    Response(404, "Session hoac san pham khong ton tai");
  }
}
