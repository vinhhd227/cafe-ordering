using Api.Core.Interfaces;
using Api.UseCases.Auth.UpdateAvatar;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Users;

public sealed class UploadAvatarRequest
{
  /// <summary>User ID from route segment {id}.</summary>
  public Guid Id { get; set; }

  /// <summary>Avatar image file (JPEG, PNG, WEBP). Max 2 MB.</summary>
  public IFormFile File { get; set; } = null!;
}

public sealed class UploadAvatarResponse
{
  /// <summary>Public URL of the uploaded avatar.</summary>
  public string AvatarUrl { get; set; } = string.Empty;
}

public class UploadAvatarEndpoint(IMediator mediator, IFileStorageService storage)
  : Endpoint<UploadAvatarRequest, UploadAvatarResponse>
{
  private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

  public override void Configure()
  {
    Post("/api/admin/users/{id}/avatar");
    Policies("user.update");
    AllowFileUploads();
    DontAutoTag();
    Description(b => b.WithTags("Users"));
  }

  public override async Task HandleAsync(UploadAvatarRequest req, CancellationToken ct)
  {
    var file = req.File;

    if (file is null || file.Length == 0)
    {
      AddError("file", "No file provided.");
      await Send.ErrorsAsync(400, ct);
      return;
    }

    if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
    {
      AddError("file", "Only image files are allowed (JPEG, PNG, WEBP).");
      await Send.ErrorsAsync(400, ct);
      return;
    }

    if (file.Length > MaxFileSizeBytes)
    {
      AddError("file", "Avatar size must not exceed 2 MB.");
      await Send.ErrorsAsync(400, ct);
      return;
    }

    await using var stream = file.OpenReadStream();
    var avatarUrl = await storage.UploadAsync(stream, file.FileName, file.ContentType, ct);

    var result = await mediator.Send(new UpdateAvatarCommand(req.Id, avatarUrl), ct);
    if (!result.IsSuccess)
    {
      await this.SendResultAsync(result, ct);
      return;
    }

    await Send.OkAsync(new UploadAvatarResponse { AvatarUrl = avatarUrl }, ct);
  }
}

public class UploadAvatarSummary : Summary<UploadAvatarEndpoint>
{
  public UploadAvatarSummary()
  {
    Summary = "Upload avatar for a user";
    Description = "Uploads an avatar image (JPEG, PNG, WEBP, max 2 MB) and saves the URL to the user's profile.";
    Response<UploadAvatarResponse>(StatusCodes.Status200OK, "Avatar uploaded successfully");
    Response(400, "Invalid file (missing, not an image, or too large)");
    Response(401, "Unauthorized");
    Response(403, "Forbidden");
    Response(404, "User not found");
  }
}
