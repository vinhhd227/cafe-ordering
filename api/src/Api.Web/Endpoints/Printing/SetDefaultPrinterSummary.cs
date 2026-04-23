namespace Api.Web.Endpoints.Printing;

public class SetDefaultPrinterSummary : Summary<SetDefaultPrinter>
{
  public SetDefaultPrinterSummary()
  {
    Summary = "Đặt máy in mặc định";
    Description = "Đặt máy in này làm mặc định cho role của nó. Máy in cũ sẽ bị bỏ mặc định.";
    Response(204, "Thành công");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
    Response(404, "Máy in không tồn tại");
  }
}
