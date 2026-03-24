using Api.UseCases.Tables.DTOs;

namespace Api.Web.Endpoints.Tables;

public class RegenerateTableQrTokenSummary : Summary<RegenerateTableQrToken>
{
  public RegenerateTableQrTokenSummary()
  {
    Summary = "Regenerate QR token for a table";
    Description =
      "Generates a new QR token for the specified table, invalidating any previously printed QR codes. " +
      "Use this when a QR code has been photographed or shared outside the cafe. " +
      "After regenerating, print and replace the QR sticker on the physical table. " +
      "Requires the table.update permission.";

    Params["TableId"] = "The integer ID of the table.";

    Response<TableDto>(200, "New token generated. Response includes updated TableDto with the new QrToken.");
    Response(404, "No table with the given ID was found.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires table.update).");
  }
}
