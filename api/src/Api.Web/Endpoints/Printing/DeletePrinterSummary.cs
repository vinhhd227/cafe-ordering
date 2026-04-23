namespace Api.Web.Endpoints.Printing;

public class DeletePrinterSummary : Summary<DeletePrinter>
{
  public DeletePrinterSummary()
  {
    Summary = "Xóa máy in";
    Description = "Soft delete cấu hình máy in.";
    Response(204, "Xóa thành công");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
    Response(404, "Máy in không tồn tại");
  }
}
