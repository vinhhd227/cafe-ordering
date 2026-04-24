using Api.UseCases.Printing.DTOs;

namespace Api.Web.Endpoints.Printing;

public class PrintBillSummary : Summary<PrintBillEndpoint>
{
  public PrintBillSummary()
  {
    Summary = "In hóa đơn cho order";
    Description = "Gửi lệnh in hóa đơn (bill) tới máy in có role RECEIPT. Nếu không chỉ định PrinterId thì dùng máy in mặc định.";
    Response<PrintLabelsResultDto>(200, "In thành công hoặc trả về bytes cho WebUSB");
    Response(400, "Dữ liệu không hợp lệ");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
    Response(404, "Order hoặc printer không tồn tại");
    Response(500, "Không in được");
  }
}
