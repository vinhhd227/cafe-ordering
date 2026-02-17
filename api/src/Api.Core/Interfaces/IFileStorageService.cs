namespace Api.Core.Interfaces;

/// <summary>
///   Service lưu trữ file (local, S3, Azure Blob...)
/// </summary>
public interface IFileStorageService
{
  /// <summary>
  ///   Upload file, trả về URL/path của file đã lưu
  /// </summary>
  Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

  /// <summary>
  ///   Xóa file theo URL/path
  /// </summary>
  Task DeleteAsync(string fileUrl, CancellationToken ct = default);

  /// <summary>
  ///   Kiểm tra file có tồn tại không
  /// </summary>
  Task<bool> ExistsAsync(string fileUrl, CancellationToken ct = default);
}
