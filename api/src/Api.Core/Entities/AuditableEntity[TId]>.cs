namespace Api.Core.Entities;

/// <summary>
///   Base entity với audit tracking.
///   Audit fields được set tự động qua DbContext SaveChangesAsync.
/// </summary>
public abstract class AuditableEntity<TId> : BaseEntity<TId>
  where TId : notnull
{
  /// <summary>
  ///   Timestamp khi entity được tạo
  /// </summary>
  public DateTime CreatedAt { get; private set; }

  /// <summary>
  ///   User đã tạo entity
  /// </summary>
  public string? CreatedBy { get; private set; }

  /// <summary>
  ///   Timestamp lần update cuối cùng
  /// </summary>
  public DateTime? UpdatedAt { get; private set; }

  /// <summary>
  ///   User đã update entity lần cuối
  /// </summary>
  public string? UpdatedBy { get; private set; }

  /// <summary>
  ///   Concurrency token cho optimistic locking
  /// </summary>
  public byte[]? RowVersion { get; private set; }
}
