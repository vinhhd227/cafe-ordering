using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class CreateManualOrderSummary : Summary<CreateManualOrder>
{
  public CreateManualOrderSummary()
  {
    Summary = "Tạo order thủ công (admin)";
    Description =
      "Tạo order thủ công cho một bàn, cho phép chỉ định thời gian, trạng thái và thanh toán tuỳ ý. " +
      "Giá sản phẩm luôn lấy từ DB. Yêu cầu quyền Admin.";

    ExampleRequest = new CreateManualOrderRequest
    {
      TableId = 1,
      Items =
      [
        new ManualOrderItemRequest { ProductId = 1, Quantity = 2, SelectedOptionValueIds = [3, 7], IsTakeaway = false },
        new ManualOrderItemRequest { ProductId = 3, Quantity = 1, IsTakeaway = true, Note = "Ít ngọt" }
      ],
      GuestCount = 2,
      Status = "COMPLETED",
      PaymentStatus = "PAID",
      PaymentMethod = "CASH",
      AmountReceived = 100000,
      TipAmount = 0
    };

    Response<OrderDto>(200, "Order được tạo thành công.");
    Response(400, "Dữ liệu không hợp lệ.");
    Response(404, "Bàn hoặc sản phẩm không tìm thấy.");
    Response(401, "Chưa xác thực.");
    Response(403, "Không có quyền.");
  }
}
