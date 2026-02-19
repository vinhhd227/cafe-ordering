using Api.UseCases.Products.DTOs;
using Ardalis.Result;

namespace Api.Web.Endpoints.Products;

public class ListSummary : Summary<List>
{
  public ListSummary()
  {
    Summary = "List products with pagination";
    Description =
      "Returns a paginated list of active products. " +
      "Results can be filtered by name using the optional `searchTerm` query parameter. " +
      "The response includes pagination metadata (current page, page size, total pages, total records) " +
      "so clients can implement standard paging controls.";

    Params["Page"] = "1-based page index (default: 1).";
    Params["PageSize"] = "Number of items per page (default: 10).";
    Params["SearchTerm"] = "Optional name filter — case-insensitive partial match.";

    Response<PagedResult<ProductSummaryDto>>(200, "Returns a paged collection of product summaries along with pagination metadata.");
    Response(400, "Invalid pagination parameters (e.g. page < 1 or pageSize < 1).");
  }
}
