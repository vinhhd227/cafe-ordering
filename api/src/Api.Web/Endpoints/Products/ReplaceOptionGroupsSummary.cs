namespace Api.Web.Endpoints.Products;

public class ReplaceAttributeGroupsSummary : Summary<ReplaceAttributeGroupsEndpoint>
{
  public ReplaceAttributeGroupsSummary()
  {
    Summary = "Cập nhật attribute groups của sản phẩm";
    Description = "Xóa toàn bộ attribute groups hiện tại và thay bằng danh sách mới. Dùng để cấu hình size, nhiệt độ, v.v.";
    Response(204, "Cập nhật thành công");
    Response(400, "Dữ liệu không hợp lệ");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
    Response(404, "Không tìm thấy sản phẩm");
  }
}
