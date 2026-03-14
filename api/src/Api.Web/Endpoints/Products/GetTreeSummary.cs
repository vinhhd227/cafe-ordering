using Api.UseCases.Products.Tree;

namespace Api.Web.Endpoints.Products;

public class GetTreeSummary : Summary<GetTree>
{
  public GetTreeSummary()
  {
    Summary = "Get active products grouped by category (tree)";
    Description =
      "Returns all active products grouped by their parent category. " +
      "Intended for use in promotion forms where products need to be selected " +
      "via a tree structure (category → products).";

    Response<List<ProductTreeCategoryDto>>(200, "List of categories, each containing their active products.");
    Response(401, "Unauthorized — valid JWT required.");
    Response(403, "Forbidden — requires product.read permission.");
  }
}
