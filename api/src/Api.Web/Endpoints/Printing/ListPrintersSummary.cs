using Api.UseCases.Printing.DTOs;

namespace Api.Web.Endpoints.Printing;

public class ListPrintersSummary : Summary<ListPrinters>
{
  public ListPrintersSummary()
  {
    Summary = "Danh sách máy in";
    Description = "Lấy toàn bộ cấu hình máy in, sắp xếp theo role rồi default đầu tiên.";
    Response<List<PrinterConfigDto>>(200, "Danh sách máy in");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
  }
}
