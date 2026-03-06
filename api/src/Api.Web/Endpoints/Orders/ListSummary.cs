namespace Api.Web.Endpoints.Orders;

public class ListSummary : Summary<ListOrders>
{
  public ListSummary()
  {
    Summary = "List orders with optional filters";
    Description =
      "Returns a paginated list of orders. All filters are applied server-side for accurate pagination. " +
      "Requires Staff or Admin role.";

    Params["status"]        = "Optional. Filter by order status: PENDING, PROCESSING, COMPLETED, CANCELLED.";
    Params["paymentStatus"] = "Optional. Filter by payment status: UNPAID, PAID, REFUNDED, VOIDED.";
    Params["orderNumber"]   = "Optional. Partial match on order number (e.g. 'ORD-0').";
    Params["minAmount"]     = "Optional. Include orders with total amount >= this value.";
    Params["maxAmount"]     = "Optional. Include orders with total amount <= this value.";
    Params["tableCode"]     = "Optional. Partial match on table code (e.g. 'T0' matches T01, T02).";
    Params["dateFrom"]      = "Optional. Include orders on or after this date (ISO 8601).";
    Params["dateTo"]        = "Optional. Include orders on or before this date (ISO 8601).";
    Params["page"]          = "Page number, 1-based. Defaults to 1.";
    Params["pageSize"]      = "Number of items per page. Defaults to 20.";

    Response(200, "Paginated list of orders.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
