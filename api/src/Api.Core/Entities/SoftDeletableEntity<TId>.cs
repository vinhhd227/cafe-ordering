namespace Api.Core.Entities;

/// <summary>
///   Base entity với soft delete support
/// </summary>
public abstract class SoftDeletableEntity<TId> : AuditableEntity<TId>
  where TId : notnull
{
  /// <summary>
  ///   Flag đánh dấu entity đã bị xóa
  /// </summary>
  public bool IsDeleted { get; private set; }

  /// <summary>
  ///   Timestamp khi entity bị xóa
  /// </summary>
  public DateTime? DeletedAt { get; private set; }

  /// <summary>
  ///   User đã xóa entity
  /// </summary>
  public string? DeletedBy { get; private set; }

  /// <summary>
  ///   Soft delete entity
  /// </summary>
  public virtual void Delete(string deletedBy)
  {
    if (IsDeleted)
    {
      throw new InvalidOperationException($"{GetType().Name} is already deleted");
    }

    IsDeleted = true;
    DeletedAt = DateTime.UtcNow;
    DeletedBy = deletedBy;

    // Raise domain event
    RegisterDomainEvent(new EntityDeletedEvent(
      Convert.ToInt32(Id), GetType().Name, deletedBy));
  }

  /// <summary>
  ///   Restore deleted entity
  /// </summary>
  public virtual void Restore()
  {
    if (!IsDeleted)
    {
      throw new InvalidOperationException($"{GetType().Name} is not deleted");
    }

    IsDeleted = false;
    DeletedAt = null;
    DeletedBy = null;

    // Raise domain event
    RegisterDomainEvent(new EntityRestoredEvent(
      Convert.ToInt32(Id), GetType().Name));
  }
}
