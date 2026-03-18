namespace Api.Web.Endpoints.Uploads;

public class UploadImageSummary : Summary<UploadImage>
{
  public UploadImageSummary()
  {
    Summary = "Upload a product image";
    Description =
      "Accepts a multipart/form-data request containing a single image file (JPEG, PNG, WEBP, or GIF, max 5 MB). " +
      "Saves the file to the configured storage folder and returns the public URL that can be stored " +
      "as a product's ImageUrl. In production the URL points to the Nginx static-file server; " +
      "in local development it is served directly by ASP.NET Core.";

    Response<UploadImageResponse>(200, "Image uploaded successfully. Returns the public URL.");
    Response(400, "Validation failed — file missing, wrong content type, or exceeds 5 MB size limit.");
    Response(401, "Authentication required.");
    Response(403, "Caller does not have the 'product.update' permission.");
  }
}
