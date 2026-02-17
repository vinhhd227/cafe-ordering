namespace Api.Core.Interfaces;

/// <summary>
///   Cung cấp thông tin user hiện tại (từ JWT/Cookie/Session).
///   Dùng cho auto audit (CreatedBy, UpdatedBy).
/// </summary>
public interface ICurrentUserService
{
  /// <summary>
  ///   Id của user hiện tại (null nếu chưa đăng nhập)
  /// </summary>
  string? UserId { get; }

  /// <summary>
  ///   Username hoặc email của user hiện tại
  /// </summary>
  string? UserName { get; }

  /// <summary>
  ///   User đã đăng nhập chưa
  /// </summary>
  bool IsAuthenticated { get; }
}
