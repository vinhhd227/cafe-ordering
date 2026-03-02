using Api.UseCases.Tables.DTOs;

namespace Api.FunctionalTests.Tables;

[Collection(nameof(FunctionalTestCollection))]
public class TableEndpointTests
{
  private readonly HttpClient _client;
  private readonly HttpClient _adminClient;
  private readonly ITestOutputHelper _output;

  public TableEndpointTests(ApiFactory factory, ITestOutputHelper output)
  {
    _client      = factory.CreateClient();
    _adminClient = factory.CreateAdminClient();
    _output      = output;
  }

  // GET /api/admin/tables
  [Fact]
  public async Task ListTables_WithAdminToken_ReturnsTables()
  {
    var result = await _adminClient.GetAndDeserializeAsync<List<TableDto>>(
      "/api/admin/tables", _output);

    result.Should().NotBeNull();
    result.Should().NotBeEmpty();
  }

  [Fact]
  public async Task ListTables_WithoutAuth_ReturnsUnauthorized()
  {
    await _client.GetAndEnsureUnauthorizedAsync("/api/admin/tables", _output);
  }

  // POST /api/admin/tables
  [Fact]
  public async Task CreateTable_WithAdminToken_ReturnsCreatedTable()
  {
    // Use a large unique number to avoid conflicts with seeded data (1-5) or prior test runs
    var number = Random.Shared.Next(10_000, 99_999);
    var code   = $"T{number}";

    var result = await _adminClient.PostAndDeserializeAsync<TableDto>(
      "/api/admin/tables", JsonContent.Create(new { Number = number, Code = code }), _output);

    result.Should().NotBeNull();
    result.Number.Should().Be(number);
    result.Code.Should().Be(code);
  }

  [Fact]
  public async Task CreateTable_WithoutAuth_ReturnsUnauthorized()
  {
    var response = await _client.PostAsync(
      "/api/admin/tables", JsonContent.Create(new { Number = 98, Code = "T98" }));

    response.EnsureUnauthorized();
  }

  [Fact]
  public async Task CreateTable_WithDuplicateNumber_ReturnsBadRequest()
  {
    // Table 1 already seeded
    await _adminClient.PostAndEnsureBadRequestAsync(
      "/api/admin/tables", JsonContent.Create(new { Number = 1, Code = "DUPLICATE" }), _output);
  }
}
