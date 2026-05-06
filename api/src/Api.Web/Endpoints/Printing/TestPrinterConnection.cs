using Api.UseCases.Printing.TestConnection;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Printing;

public sealed class TestPrinterRequest
{
  public int Id { get; set; }
}

public sealed class TestPrinterResponse
{
  public bool    Success { get; set; }
  public string? Error   { get; set; }
}

public class TestPrinterConnection(IMediator mediator) : Endpoint<TestPrinterRequest, TestPrinterResponse>
{
  public override void Configure()
  {
    Post("/api/admin/printers/{Id}/test");
    Policies("printer.update");
    DontAutoTag();
    Description(b => b.WithTags("Printing"));
  }

  public override async Task HandleAsync(TestPrinterRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new TestPrinterConnectionCommand(req.Id), ct);
    if (!result.IsSuccess)
    {
      await this.SendResultAsync(result, ct);
      return;
    }

    var ok = result.Value;
    await Send.OkAsync(new TestPrinterResponse { Success = ok, Error = ok ? null : "Connection failed." }, cancellation: ct);
  }
}
