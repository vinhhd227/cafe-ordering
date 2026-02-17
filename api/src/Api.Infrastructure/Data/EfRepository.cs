namespace Api.Infrastructure.Data;

/// <summary>
///   Generic repository (read + write)
/// </summary>
public class EfRepository<T> : RepositoryBase<T>, IRepository<T>
  where T : class, IAggregateRoot
{
  public EfRepository(AppDbContext dbContext) : base(dbContext)
  {
  }
}

/// <summary>
///   Read-only repository
/// </summary>
public class EfReadRepository<T> : RepositoryBase<T>, IReadRepository<T>
  where T : class, IAggregateRoot
{
  public EfReadRepository(AppDbContext dbContext) : base(dbContext)
  {
  }
}
