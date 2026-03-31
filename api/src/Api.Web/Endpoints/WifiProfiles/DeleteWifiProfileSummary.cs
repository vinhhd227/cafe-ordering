namespace Api.Web.Endpoints.WifiProfiles;

public class DeleteWifiProfileSummary : Summary<DeleteWifiProfile>
{
  public DeleteWifiProfileSummary()
  {
    Summary = "Delete a WiFi QR profile";
    Description = "Soft-deletes a saved WiFi QR profile by ID. Requires utility.read permission.";
    Response(204, "Profile deleted successfully.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires utility.delete).");
    Response(404, "Profile not found.");
  }
}
