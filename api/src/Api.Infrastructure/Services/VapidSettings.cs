namespace Api.Infrastructure.Services;

public class VapidSettings
{
    public string Subject { get; set; } = default!;
    public string PublicKey { get; set; } = default!;
    public string PrivateKey { get; set; } = default!;
}
