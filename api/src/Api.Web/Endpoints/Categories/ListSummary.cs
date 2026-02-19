using Api.UseCases.Categories.DTOs;

namespace Api.Web.Endpoints.Categories;

public class ListSummary : Summary<List>
{
  public ListSummary()
  {
    Summary = "List all categories";
    Description =
      "Returns all categories, optionally filtered to active ones only. " +
      "Active categories are those whose products are currently available on the menu. " +
      "Public-facing clients should typically pass `activeOnly=true`; " +
      "admin dashboards can omit the filter to manage inactive categories as well.";

    Params["ActiveOnly"] = "Pass `true` to return only active categories. Defaults to `false` (return all).";

    Response<List<CategoryDto>>(200, "Returns a flat list of categories ordered by ID.");
  }
}
