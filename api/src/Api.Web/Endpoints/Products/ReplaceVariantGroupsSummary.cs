namespace Api.Web.Endpoints.Products;

public class ReplaceVariantGroupsSummary : Summary<ReplaceVariantGroupsEndpoint>
{
  public ReplaceVariantGroupsSummary()
  {
    Summary = "Cáº­p nháº­t variant option groups cá»§a sáº£n pháº©m";
    Description = "XÃ³a toÃ n bá»™ variant option groups hiá»‡n táº¡i vÃ  thay báº±ng danh sÃ¡ch má»›i. DÃ¹ng Ä‘á»ƒ cáº¥u hÃ¬nh size, nhiá»‡t Ä‘á»™, v.v.";
    Response(204, "Cáº­p nháº­t thÃ nh cÃ´ng");
    Response(400, "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡");
    Response(401, "ChÆ°a xÃ¡c thá»±c");
    Response(403, "KhÃ´ng cÃ³ quyá»n");
    Response(404, "KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m");
  }
}
