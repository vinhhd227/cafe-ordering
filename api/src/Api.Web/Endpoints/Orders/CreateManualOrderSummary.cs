using Api.UseCases.Orders.DTOs;

namespace Api.Web.Endpoints.Orders;

public class CreateManualOrderSummary : Summary<CreateManualOrder>
{
  public CreateManualOrderSummary()
  {
    Summary = "Táº¡o order thá»§ cÃ´ng (admin)";
    Description =
      "Táº¡o order thá»§ cÃ´ng cho má»™t bÃ n, cho phÃ©p chá»‰ Ä‘á»‹nh thá»i gian, tráº¡ng thÃ¡i vÃ  thanh toÃ¡n tuá»³ Ã½. " +
      "GiÃ¡ sáº£n pháº©m luÃ´n láº¥y tá»« DB. YÃªu cáº§u quyá»n Admin.";

    ExampleRequest = new CreateManualOrderRequest
    {
      TableId = 1,
      Items =
      [
        new ManualOrderItemRequest { ProductId = 1, Quantity = 2, SelectedVariantValueIds = [3, 7], IsTakeaway = false },
        new ManualOrderItemRequest { ProductId = 3, Quantity = 1, IsTakeaway = true, Note = "Ãt ngá»t" }
      ],
      GuestCount = 2,
      Status = "COMPLETED",
      PaymentStatus = "PAID",
      PaymentMethod = "CASH",
      AmountReceived = 100000,
      TipAmount = 0
    };

    Response<OrderDto>(200, "Order Ä‘Æ°á»£c táº¡o thÃ nh cÃ´ng.");
    Response(400, "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡.");
    Response(404, "BÃ n hoáº·c sáº£n pháº©m khÃ´ng tÃ¬m tháº¥y.");
    Response(401, "ChÆ°a xÃ¡c thá»±c.");
    Response(403, "KhÃ´ng cÃ³ quyá»n.");
  }
}
