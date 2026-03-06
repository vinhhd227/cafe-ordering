namespace Api.Web.Endpoints.Orders;

public class UpdateOrderItemSummary : Summary<UpdateOrderItem>
{
  public UpdateOrderItemSummary()
  {
    Summary = "Set the quantity of an item in an order (staff)";
    Description =
      "Sets the quantity of a product in an existing order. " +
      "A quantity of 0 removes the item. If the product is not yet in the order, it is added. " +
      "Only works on Pending orders with Unpaid payment status. " +
      "Requires authentication (Staff or Admin).";

    Params["orderId"]   = "The integer ID of the order.";
    Params["productId"] = "The integer ID of the product to update.";

    Response(200, "Updated order details returned.");
    Response(400, "Order is not in Pending/Unpaid state.");
    Response(404, "Order or product not found.");
    Response(409, "Edit not allowed in the current state.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
