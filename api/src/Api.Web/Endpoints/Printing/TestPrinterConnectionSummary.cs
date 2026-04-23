namespace Api.Web.Endpoints.Printing;

public class TestPrinterConnectionSummary : Summary<TestPrinterConnection>
{
  public TestPrinterConnectionSummary()
  {
    Summary = "Test kết nối máy in";
    Description = "In trang test để kiểm tra kết nối. WebUSB transport luôn trả về success=true.";
    Response<TestPrinterResponse>(200, "Kết quả kiểm tra");
    Response(401, "Chưa xác thực");
    Response(403, "Không có quyền");
    Response(404, "Máy in không tồn tại");
  }
}
