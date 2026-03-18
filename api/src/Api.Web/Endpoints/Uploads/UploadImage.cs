using Api.Core.Interfaces;

namespace Api.Web.Endpoints.Uploads;

public sealed class UploadImageRequest
{
  /// <summary>Image file to upload (JPEG, PNG, WEBP, GIF). Max 5 MB.</summary>
  public IFormFile File { get; set; } = null!;
}

public sealed class UploadImageResponse
{
  /// <summary>Public URL of the uploaded image.</summary>
  public string Url { get; set; } = string.Empty;
}

public class UploadImage(IFileStorageService storage) : Endpoint<UploadImageRequest, UploadImageResponse>
{
  private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

  public override void Configure()
  {
    Post("/api/admin/uploads/image");
    Policies("product.update");
    AllowFileUploads();
    DontAutoTag();
    Description(b => b.WithTags("Uploads"));
  }

  public override async Task HandleAsync(UploadImageRequest req, CancellationToken ct)
  {
    var file = req.File;

    if (file is null || file.Length == 0)
    {
      AddError("file", "No file provided.");
      await SendErrorsAsync(400, ct);
      return;
    }

    if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
    {
      AddError("file", "Only image files are allowed (JPEG, PNG, WEBP, GIF).");
      await SendErrorsAsync(400, ct);
      return;
    }

    if (file.Length > MaxFileSizeBytes)
    {
      AddError("file", "File size must not exceed 5 MB.");
      await SendErrorsAsync(400, ct);
      return;
    }

    await using var stream = file.OpenReadStream();
    var url = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

    await SendAsync(new UploadImageResponse { Url = url }, StatusCodes.Status200OK, ct);
  }
}
