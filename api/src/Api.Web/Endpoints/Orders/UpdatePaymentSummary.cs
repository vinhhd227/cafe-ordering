namespace Api.Web.Endpoints.Orders;

public class UpdatePaymentSummary : Summary<UpdatePayment>
{
  public UpdatePaymentSummary()
  {
    Summary = "Update the payment details of an order";
    Description =
      "Sets the payment status, method, amount received, and optional tip for an order. " +
      "Valid payment statuses: Paid, Refunded, Voided. Valid payment methods: Cash, Card, Transfer, Unknown. " +
      "After any payment update, an auto-close check is triggered: if all orders in the session are resolved " +
      "(paid or cancelled), the session is automatically closed and the table moves to Cleaning. " +
      "Requires Staff or Admin role.";

    Params["id"] = "The integer ID of the order.";

    Response(200, "Payment updated successfully.");
    Response(400, "Invalid payment status or payment method value.");
    Response(404, "Order not found.");
    Response(409, "Payment update is not allowed in the current state (e.g. PaymentMethod required when Paid).");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions.");
  }
}
