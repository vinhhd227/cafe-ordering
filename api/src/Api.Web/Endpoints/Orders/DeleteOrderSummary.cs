namespace Api.Web.Endpoints.Orders;

public class DeleteOrderSummary : Summary<DeleteOrder>
{
  public DeleteOrderSummary()
  {
    Summary = "Xoá đơn đã huỷ";
    Description = "Xoá vĩnh viễn một order có trạng thái CANCELLED. Tất cả items và promotions liên quan sẽ bị xoá theo.";

    Params["id"] = "Integer ID của order.";

    Response(204, "Xoá thành công.");
    Response(400, "Order không ở trạng thái CANCELLED.");
    Response(401, "Chưa xác thực.");
    Response(403, "Không có quyền.");
    Response(404, "Không tìm thấy order.");
  }
}
