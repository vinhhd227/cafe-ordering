namespace Api.Core.Aggregates.SavedMenuAggregate;

public class SavedMenu : SoftDeletableEntity<int>, IAggregateRoot
{
  private SavedMenu() { }

  public string Name { get; private set; } = string.Empty;
  public string CafeName { get; private set; } = string.Empty;
  public string Slogan { get; private set; } = string.Empty;
  public string PrimaryColor { get; private set; } = "#10b981";
  public string Layout { get; private set; } = "2col";
  public string MenuTemplate { get; private set; } = "default";
  public string MenuFont { get; private set; } = "system-ui, sans-serif";
  public string CategoryOrderJson { get; private set; } = "[]";
  public string SelectedProductIdsJson { get; private set; } = "[]";

  public static SavedMenu Create(
    string name,
    string cafeName,
    string slogan,
    string primaryColor,
    string layout,
    string menuTemplate,
    string menuFont,
    string categoryOrderJson,
    string selectedProductIdsJson)
  {
    return new SavedMenu
    {
      Name                  = Guard.Against.NullOrWhiteSpace(name),
      CafeName              = cafeName,
      Slogan                = slogan,
      PrimaryColor          = Guard.Against.NullOrWhiteSpace(primaryColor),
      Layout                = Guard.Against.NullOrWhiteSpace(layout),
      MenuTemplate          = Guard.Against.NullOrWhiteSpace(menuTemplate),
      MenuFont              = Guard.Against.NullOrWhiteSpace(menuFont),
      CategoryOrderJson     = categoryOrderJson,
      SelectedProductIdsJson = selectedProductIdsJson,
    };
  }

  public void Update(
    string name,
    string cafeName,
    string slogan,
    string primaryColor,
    string layout,
    string menuTemplate,
    string menuFont,
    string categoryOrderJson,
    string selectedProductIdsJson)
  {
    Name                  = Guard.Against.NullOrWhiteSpace(name);
    CafeName              = cafeName;
    Slogan                = slogan;
    PrimaryColor          = Guard.Against.NullOrWhiteSpace(primaryColor);
    Layout                = Guard.Against.NullOrWhiteSpace(layout);
    MenuTemplate          = Guard.Against.NullOrWhiteSpace(menuTemplate);
    MenuFont              = Guard.Against.NullOrWhiteSpace(menuFont);
    CategoryOrderJson     = categoryOrderJson;
    SelectedProductIdsJson = selectedProductIdsJson;
  }
}
