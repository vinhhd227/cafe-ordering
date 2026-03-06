namespace Api.Web.Endpoints.Orders;

public class MergeOrdersSummary : Summary<MergeOrders>
{
  public MergeOrdersSummary()
  {
    Summary = "Merge multiple orders into one";
    Description =
      "Moves all items from secondary orders into the primary order. " +
      "Secondary orders are cancelled after merging. " +
      "All involved orders must have Unpaid payment status. " +
      "Requires Staff or Admin role.";

    Response(204, "Orders merged successfully.");
    Response(400, "Validation error: invalid order IDs or payment status.");
    Response(404, "Primary or secondary order not found.");
    Response(409, "Merge not allowed in the current state.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
