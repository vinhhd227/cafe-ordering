namespace Api.Web.Endpoints.Orders;

public class UpdateOrderItemPublicSummary : Summary<UpdateOrderItemPublic>
{
  public UpdateOrderItemPublicSummary()
  {
    Summary = "Set the quantity of an item in an order (guest)";
    Description =
      "Customer-facing endpoint to update item quantity in an existing order. " +
      "A quantity of 0 removes the item. If the product is not yet in the order, it is added. " +
      "The sessionId in the request body must match the order's session (ownership check). " +
      "Only works on Pending orders with Unpaid payment status. " +
      "No authentication required.";

    Params["orderId"]   = "The integer ID of the order.";
    Params["productId"] = "The integer ID of the product to update.";

    Response(200, "Updated order details returned.");
    Response(400, "Order is not in Pending/Unpaid state.");
    Response(403, "Session ID does not match the order's session.");
    Response(404, "Order or product not found.");
    Response(409, "Edit not allowed in the current state.");
  }
}
