using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Core.Entities;

/// <summary>
///   Generic base entity with a custom Id type
/// </summary>
/// <typeparam name="TId">Type of Id (int, Guid, long, string...)</typeparam>
public abstract class BaseEntity<TId> : IHasDomainEvents, IEquatable<BaseEntity<TId>>
  where TId : notnull
{
  /// <summary>
  ///   Domain events collection
  ///   NotMapped để EF Core không persist vào DB
  /// </summary>
  [NotMapped] private readonly List<DomainEventBase> _domainEvents = new();

  /// <summary>
  ///   Primary key
  /// </summary>
  public TId Id { get; protected set; } = default!;

  /// <summary>
  ///   Read-only access to domain events
  /// </summary>
  [NotMapped]
  public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

  /// <summary>
  ///   Check if entity has domain events
  /// </summary>
  [NotMapped]
  public bool HasDomainEvents => _domainEvents.Any();

  #region Helper Methods

  /// <summary>
  ///   Check if the entity is transient (not yet persisted)
  /// </summary>
  public bool IsTransient()
  {
    if (Id is int intId)
    {
      return intId == 0;
    }

    if (Id is long longId)
    {
      return longId == 0;
    }

    if (Id is Guid guidId)
    {
      return guidId == Guid.Empty;
    }

    if (Id is string stringId)
    {
      return string.IsNullOrEmpty(stringId);
    }

    return EqualityComparer<TId>.Default.Equals(Id, default!);
  }

  #endregion

  /// <summary>
  ///   String representation
  /// </summary>
  public override string ToString()
  {
    return $"{GetType().Name} [Id={Id}]";
  }

  #region Domain Events Management

  /// <summary>
  ///   Register a domain event
  /// </summary>
  protected void RegisterDomainEvent(DomainEventBase domainEvent)
  {
    _domainEvents.Add(domainEvent);
  }

  /// <summary>
  ///   Clear all domain events
  /// </summary>
  public void ClearDomainEvents()
  {
    _domainEvents.Clear();
  }

  /// <summary>
  ///   Remove a specific domain event
  /// </summary>
  protected void RemoveDomainEvent(DomainEventBase domainEvent)
  {
    _domainEvents.Remove(domainEvent);
  }

  #endregion

  #region Equality Comparison

  /// <summary>
  ///   Equality comparison based on Id
  /// </summary>
  public override bool Equals(object? obj)
  {
    if (obj is not BaseEntity<TId> other)
    {
      return false;
    }

    if (ReferenceEquals(this, other))
    {
      return true;
    }

    if (GetType() != other.GetType())
    {
      return false;
    }

    if (IsTransient() || other.IsTransient())
    {
      return false;
    }

    return Id.Equals(other.Id);
  }

  /// <summary>
  ///   Type-safe equality comparison
  /// </summary>
  public bool Equals(BaseEntity<TId>? other)
  {
    if (other is null)
    {
      return false;
    }

    if (ReferenceEquals(this, other))
    {
      return true;
    }

    if (GetType() != other.GetType())
    {
      return false;
    }

    if (IsTransient() || other.IsTransient())
    {
      return false;
    }

    return Id.Equals(other.Id);
  }

  /// <summary>
  ///   Equality operator
  /// </summary>
  public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right)
  {
    if (left is null && right is null)
    {
      return true;
    }

    if (left is null || right is null)
    {
      return false;
    }

    return left.Equals(right);
  }

  /// <summary>
  ///   Inequality operator
  /// </summary>
  public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right)
  {
    return !(left == right);
  }

  /// <summary>
  ///   Hash code based on type and Id
  /// </summary>
  public override int GetHashCode()
  {
    return HashCode.Combine(GetType(), Id);
  }

  #endregion
}
