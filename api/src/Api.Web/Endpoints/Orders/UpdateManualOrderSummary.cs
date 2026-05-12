using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class UpdateManualOrderSummary : Summary<UpdateManualOrder>
{
  public UpdateManualOrderSummary()
  {
    Summary = "Sửa toàn bộ order (admin)";
    Description =
      "Thay thế toàn bộ items, trạng thái, thời gian và thông tin thanh toán của một order bất kỳ. " +
      "Bypass state machine — cho phép set mọi trạng thái. " +
      "Giá sản phẩm luôn lấy từ DB, không tin client. " +
      "Yêu cầu quyền Admin.";

    ExampleRequest = new UpdateManualOrderRequest
    {
      Id = 42,
      Items =
      [
        new ManualOrderItemRequest { ProductId = 1, Quantity = 2, SelectedOptionValueIds = [1, 3], IsTakeaway = false },
        new ManualOrderItemRequest { ProductId = 3, Quantity = 1, IsTakeaway = true }
      ],
      OrderedAt = null,
      GuestCount = 2,
      Status = "COMPLETED",
      PaymentStatus = "PAID",
      PaymentMethod = "CASH",
      AmountReceived = 80000,
      TipAmount = 5000
    };

    Response<OrderDto>(200, "Order được cập nhật thành công.");
    Response(400, "Dữ liệu không hợp lệ: items rỗng, status/payment enum không đúng.");
    Response(404, "Order hoặc sản phẩm không tìm thấy.");
    Response(401, "Chưa xác thực.");
    Response(403, "Không có quyền.");
  }
}
