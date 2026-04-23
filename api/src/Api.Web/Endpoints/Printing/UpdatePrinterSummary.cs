using Api.UseCases.Printing.DTOs;

namespace Api.Web.Endpoints.Printing;

public class UpdatePrinterSummary : Summary<UpdatePrinter>
{
  public UpdatePrinterSummary()
  {
    Summary = "Cập nhật máy in";
    Description = "Cập nhật cấu hình máy in. Role không thể thay đổi sau khi tạo.";
    Response<PrinterConfigDto>(200, "Cập nhật thành công");
    Response(400, "Dữ liệu không hợp lệ");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
    Response(404, "Máy in không tồn tại");
  }
}
