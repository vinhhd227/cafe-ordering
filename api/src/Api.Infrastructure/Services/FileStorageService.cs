namespace Api.Infrastructure.Services;

/// <summary>
///   Cấu hình file storage
/// </summary>
public class FileStorageSettings
{
  /// <summary>
  ///   Thư mục gốc lưu file (local path)
  /// </summary>
  public string BasePath { get; set; } = "uploads";

  /// <summary>
  ///   Base URL để trả về client (e.g., "https://yourdomain.com/uploads")
  /// </summary>
  public string BaseUrl { get; set; } = "/uploads";
}

/// <summary>
///   Lưu file vào local filesystem.
///   Có thể swap sang S3/Azure Blob bằng cách tạo implementation khác.
/// </summary>
public class FileStorageService : IFileStorageService
{
  private readonly ILogger<FileStorageService> _logger;
  private readonly FileStorageSettings _settings;

  public FileStorageService(IOptions<FileStorageSettings> settings, ILogger<FileStorageService> logger)
  {
    _settings = settings.Value;
    _logger = logger;
  }

  public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType,
    CancellationToken ct = default)
  {
    // Tạo unique filename để tránh trùng
    var extension = Path.GetExtension(fileName);
    var uniqueName = $"{Guid.NewGuid()}{extension}";

    // Tạo thư mục theo năm/tháng
    var subFolder = DateTime.UtcNow.ToString("yyyy/MM");
    var folderPath = Path.Combine(_settings.BasePath, subFolder);
    Directory.CreateDirectory(folderPath);

    var filePath = Path.Combine(folderPath, uniqueName);

    await using var stream = new FileStream(filePath, FileMode.Create);
    await fileStream.CopyToAsync(stream, ct);

    var fileUrl = $"{_settings.BaseUrl}/{subFolder}/{uniqueName}";

    _logger.LogInformation("File uploaded: {FileUrl}", fileUrl);

    return fileUrl;
  }

  public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
  {
    var relativePath = fileUrl.Replace(_settings.BaseUrl, "").TrimStart('/');
    var filePath = Path.Combine(_settings.BasePath, relativePath);

    if (File.Exists(filePath))
    {
      File.Delete(filePath);
      _logger.LogInformation("File deleted: {FileUrl}", fileUrl);
    }

    return Task.CompletedTask;
  }

  public Task<bool> ExistsAsync(string fileUrl, CancellationToken ct = default)
  {
    var relativePath = fileUrl.Replace(_settings.BaseUrl, "").TrimStart('/');
    var filePath = Path.Combine(_settings.BasePath, relativePath);

    return Task.FromResult(File.Exists(filePath));
  }
}
