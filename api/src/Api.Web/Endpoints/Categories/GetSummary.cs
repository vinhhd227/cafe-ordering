using Api.UseCases.Categories.DTOs;

namespace Api.Web.Endpoints.Categories;

public class GetSummary : Summary<Get>
{
  public GetSummary()
  {
    Summary = "Get a category by ID";
    Description =
      "Returns the detail record for a single category, including its active/inactive status " +
      "and audit timestamps. Both active and inactive categories are returned by this endpoint; " +
      "use the list endpoint with `activeOnly=true` to filter.";

    Params["CategoryId"] = "The integer ID of the category to retrieve.";

    ResponseExamples[200] = new CategoryDto(
      Id: 1,
      Name: "Coffee",
      Description: "Espresso-based and filter coffee drinks",
      IsActive: true,
      CreatedAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
      UpdatedAt: null);

    Response<CategoryDto>(200, "Returns the category detail.");
    Response(404, "No category with the given ID was found.");
  }
}
