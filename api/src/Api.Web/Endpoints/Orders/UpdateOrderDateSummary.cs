namespace Api.Web.Endpoints.Orders;

public class UpdateOrderDateSummary : Summary<UpdateOrderDate>
{
  public UpdateOrderDateSummary()
  {
    Summary = "Cập nhật ngày đặt của order";
    Description = "Cho phép admin sửa OrderDate của order về ngày thanh toán thực tế để doanh thu được tính đúng.";

    Params["id"] = "Integer ID của order.";

    Response(204, "Cập nhật thành công.");
    Response(400, "Dữ liệu không hợp lệ.");
    Response(401, "Chưa xác thực.");
    Response(403, "Không có quyền.");
    Response(404, "Không tìm thấy order.");
  }
}
