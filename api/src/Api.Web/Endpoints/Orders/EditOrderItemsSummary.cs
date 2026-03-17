namespace Api.Web.Endpoints.Orders;

public class EditOrderItemsSummary : Summary<EditOrderItems>
{
  public EditOrderItemsSummary()
  {
    Summary = "Replace all items in a Pending order";
    Description =
      "Clears all existing items and promotions, then re-adds the provided items with current product prices. " +
      "Also updates GuestCount if supplied. " +
      "Order must be in Pending status with Unpaid payment status. " +
      "Requires Staff or Admin role.";

    Response(204, "Order items updated successfully.");
    Response(400, "Validation error: order is not editable, or item list is empty.");
    Response(404, "Order or product not found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
