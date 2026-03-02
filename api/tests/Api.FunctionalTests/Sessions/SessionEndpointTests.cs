using Api.Core.Aggregates.GuestSessionAggregate;
using Api.UseCases.Sessions.DTOs;

namespace Api.FunctionalTests.Sessions;

[Collection(nameof(FunctionalTestCollection))]
public class SessionEndpointTests
{
  private readonly HttpClient _client;
  private readonly HttpClient _adminClient;
  private readonly ITestOutputHelper _output;

  public SessionEndpointTests(ApiFactory factory, ITestOutputHelper output)
  {
    _client      = factory.CreateClient();
    _adminClient = factory.CreateAdminClient();
    _output      = output;
  }

  // GET /api/tables/{tableId}/session
  [Fact]
  public async Task GetOrCreateSession_ValidTable_ReturnsActiveSession()
  {
    var result = await _client.GetAndDeserializeAsync<SessionContextDto>(
      "/api/tables/1/session", _output);

    result.Should().NotBeNull();
    result.TableId.Should().Be(1);
    result.Status.Should().Be(GuestSessionStatus.Active);
  }

  [Fact]
  public async Task GetOrCreateSession_CalledTwice_ReturnsSameSession()
  {
    var first  = await _client.GetAndDeserializeAsync<SessionContextDto>("/api/tables/2/session", _output);
    var second = await _client.GetAndDeserializeAsync<SessionContextDto>("/api/tables/2/session", _output);

    first.SessionId.Should().Be(second.SessionId);
  }

  [Fact]
  public async Task GetOrCreateSession_InvalidTable_ReturnsNotFound()
  {
    await _client.GetAndEnsureNotFoundAsync("/api/tables/9999/session", _output);
  }

  // GET /api/sessions/{sessionId}/summary
  [Fact]
  public async Task GetSessionSummary_ValidSession_ReturnsSummary()
  {
    var session = await _client.GetAndDeserializeAsync<SessionContextDto>(
      "/api/tables/3/session", _output);

    var summary = await _client.GetAndDeserializeAsync<SessionSummaryDto>(
      $"/api/sessions/{session.SessionId}/summary", _output);

    summary.Should().NotBeNull();
    summary.SessionId.Should().Be(session.SessionId);
    summary.TableId.Should().Be(3);
  }

  [Fact]
  public async Task GetSessionSummary_InvalidSessionId_ReturnsNotFound()
  {
    await _client.GetAndEnsureNotFoundAsync(
      $"/api/sessions/{Guid.NewGuid()}/summary", _output);
  }

  // PUT /api/sessions/{sessionId}/close
  [Fact]
  public async Task CloseSession_WithAdminToken_ReturnsSuccess()
  {
    // Open a fresh session on table 4
    var session = await _client.GetAndDeserializeAsync<SessionContextDto>(
      "/api/tables/4/session", _output);

    var response = await _adminClient.PutAsync(
      $"/api/sessions/{session.SessionId}/close", JsonContent.Create(new { }));

    response.EnsureSuccessStatusCode();
  }

  [Fact]
  public async Task CloseSession_WithoutAuth_ReturnsUnauthorized()
  {
    var response = await _client.PutAsync(
      $"/api/sessions/{Guid.NewGuid()}/close", null);

    response.EnsureUnauthorized();
  }

  // POST /api/admin/sessions/counter
  [Fact]
  public async Task CreateCounterSession_WithAdminToken_ReturnsSession()
  {
    var result = await _adminClient.PostAndDeserializeAsync<SessionContextDto>(
      "/api/admin/sessions/counter", JsonContent.Create(new { }), _output);

    result.Should().NotBeNull();
    result.TableId.Should().BeNull();
    result.Status.Should().Be(GuestSessionStatus.Active);
  }

  [Fact]
  public async Task CreateCounterSession_WithoutAuth_ReturnsUnauthorized()
  {
    var response = await _client.PostAsync(
      "/api/admin/sessions/counter", JsonContent.Create(new { }));

    response.EnsureUnauthorized();
  }
}
