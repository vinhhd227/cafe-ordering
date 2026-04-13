namespace Api.Core.Aggregates.RecipeAggregate;

public class Recipe : SoftDeletableEntity<int>, IAggregateRoot
{
  private Recipe() { }

  public string Name { get; private set; } = string.Empty;
  public RecipeType Type { get; private set; }
  public RecipeCategory Category { get; private set; }

  /// <summary>Yield description, e.g. "1200ml ≈ 40 cốc"</summary>
  public string? Yield { get; private set; }

  /// <summary>Full recipe instructions in markdown/plain text</summary>
  public string Content { get; private set; } = string.Empty;

  /// <summary>Storage and usage notes</summary>
  public string? Notes { get; private set; }

  public bool IsActive { get; private set; } = true;

  public static Recipe Create(
    string name,
    RecipeType type,
    RecipeCategory category,
    string content,
    string? yield = null,
    string? notes = null)
  {
    return new Recipe
    {
      Name     = Guard.Against.NullOrWhiteSpace(name),
      Type     = type,
      Category = category,
      Content  = Guard.Against.NullOrWhiteSpace(content),
      Yield    = yield,
      Notes    = notes,
      IsActive = true,
    };
  }

  public void Update(
    string name,
    RecipeType type,
    RecipeCategory category,
    string content,
    string? yield,
    string? notes)
  {
    Name     = Guard.Against.NullOrWhiteSpace(name);
    Type     = type;
    Category = category;
    Content  = Guard.Against.NullOrWhiteSpace(content);
    Yield    = yield;
    Notes    = notes;
  }

  public void Activate()   => IsActive = true;
  public void Deactivate() => IsActive = false;
}
