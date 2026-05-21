using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class UpdateManualOrderSummary : Summary<UpdateManualOrder>
{
  public UpdateManualOrderSummary()
  {
    Summary = "Sá»­a toÃ n bá»™ order (admin)";
    Description =
      "Thay tháº¿ toÃ n bá»™ items, tráº¡ng thÃ¡i, thá»i gian vÃ  thÃ´ng tin thanh toÃ¡n cá»§a má»™t order báº¥t ká»³. " +
      "Bypass state machine â€” cho phÃ©p set má»i tráº¡ng thÃ¡i. " +
      "GiÃ¡ sáº£n pháº©m luÃ´n láº¥y tá»« DB, khÃ´ng tin client. " +
      "YÃªu cáº§u quyá»n Admin.";

    ExampleRequest = new UpdateManualOrderRequest
    {
      Id = 42,
      Items =
      [
        new ManualOrderItemRequest { ProductId = 1, Quantity = 2, SelectedVariantValueIds = [1, 3], IsTakeaway = false },
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

    Response<OrderDto>(200, "Order Ä‘Æ°á»£c cáº­p nháº­t thÃ nh cÃ´ng.");
    Response(400, "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡: items rá»—ng, status/payment enum khÃ´ng Ä‘Ãºng.");
    Response(404, "Order hoáº·c sáº£n pháº©m khÃ´ng tÃ¬m tháº¥y.");
    Response(401, "ChÆ°a xÃ¡c thá»±c.");
    Response(403, "KhÃ´ng cÃ³ quyá»n.");
  }
}
