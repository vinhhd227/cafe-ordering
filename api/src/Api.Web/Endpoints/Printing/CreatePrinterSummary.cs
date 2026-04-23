using Api.UseCases.Printing.DTOs;

namespace Api.Web.Endpoints.Printing;

public class CreatePrinterSummary : Summary<CreatePrinter>
{
  public CreatePrinterSummary()
  {
    Summary = "Thêm máy in";
    Description = "Thêm cấu hình máy in mới vào hệ thống.";
    Response<PrinterConfigDto>(200, "Tạo thành công");
    Response(400, "Dữ liệu không hợp lệ");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
  }
}
