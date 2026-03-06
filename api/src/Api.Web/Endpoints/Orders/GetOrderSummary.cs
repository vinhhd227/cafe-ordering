namespace Api.Web.Endpoints.Orders;

public class GetOrderSummary : Summary<GetOrder>
{
  public GetOrderSummary()
  {
    Summary = "Get a single order by ID";
    Description =
      "Returns the full details of an order including all items, payment info, and table code. " +
      "Requires Staff or Admin role.";

    Params["id"] = "The integer ID of the order.";

    Response(200, "Order details returned successfully.");
    Response(404, "Order not found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
