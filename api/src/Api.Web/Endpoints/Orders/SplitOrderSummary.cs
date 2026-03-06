namespace Api.Web.Endpoints.Orders;

public class SplitOrderSummary : Summary<SplitOrder>
{
  public SplitOrderSummary()
  {
    Summary = "Split items from an order into a new order";
    Description =
      "Creates a new order with the specified items (and quantities) split from the original order. " +
      "Both the original and new orders must remain with at least one item. " +
      "Only Pending orders with Unpaid payment status can be split. " +
      "Requires Staff or Admin role.";

    Params["id"] = "The integer ID of the order to split.";

    Response(200, "Split completed. Returns the new order number and ID.");
    Response(400, "Validation error: invalid items, quantities, or order state.");
    Response(404, "Order not found.");
    Response(409, "Split not allowed in the current state.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
