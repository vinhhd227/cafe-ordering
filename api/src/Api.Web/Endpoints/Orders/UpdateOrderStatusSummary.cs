namespace Api.Web.Endpoints.Orders;

public class UpdateOrderStatusSummary : Summary<UpdateOrderStatus>
{
  public UpdateOrderStatusSummary()
  {
    Summary = "Update the status of an order";
    Description =
      "Transitions an order to a new status. Valid statuses are: Processing, Completed, Cancelled. " +
      "Cancelling an order triggers an auto-close check: if all orders in the session are resolved " +
      "(paid or cancelled), the session is automatically closed and the table moves to Cleaning. " +
      "Requires Staff or Admin role.";

    Params["id"] = "The integer ID of the order.";

    Response(200, "Order status updated successfully.");
    Response(400, "Invalid or unknown status value.");
    Response(404, "Order not found.");
    Response(409, "The status transition is not allowed from the current state.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
