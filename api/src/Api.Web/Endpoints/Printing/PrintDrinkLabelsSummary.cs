using Api.UseCases.Printing.DTOs;

namespace Api.Web.Endpoints.Printing;

public class PrintDrinkLabelsSummary : Summary<PrintDrinkLabels>
{
  public PrintDrinkLabelsSummary()
  {
    Summary = "In tem đồ uống";
    Description = "In tem cho từng item trong order. Nếu transport là WebUSB, trả về base64 bytes để frontend tự gửi xuống máy in.";
    Response<PrintLabelsResultDto>(200, "Kết quả in");
    Response(400, "Không có item nào để in");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
    Response(404, "Order hoặc máy in không tồn tại");
    Response(500, "Lỗi kết nối máy in");
  }
}
