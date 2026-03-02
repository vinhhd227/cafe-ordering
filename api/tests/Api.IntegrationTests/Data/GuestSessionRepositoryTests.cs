using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Infrastructure.Data;
using Api.IntegrationTests.Fixtures;
using Ardalis.Specification.EntityFrameworkCore;

namespace Api.IntegrationTests.Data;

[Collection(nameof(DatabaseCollection))]
public class GuestSessionRepositoryTests : IAsyncLifetime
{
  private readonly AppDbContext _db;

  public GuestSessionRepositoryTests(DatabaseFixture fixture)
  {
    _db = fixture.BuildBusinessDb();
  }

  public Task InitializeAsync() => Task.CompletedTask;

  public async Task DisposeAsync()
  {
    // Clean up sessions and tables created during this test
    _db.GuestSessions.RemoveRange(_db.GuestSessions);
    _db.Tables.RemoveRange(_db.Tables);
    await _db.SaveChangesAsync();
    await _db.DisposeAsync();
  }

  [Fact]
  public async Task ActiveSessionByTableIdSpec_ShouldReturnOnlyActiveSession()
  {
    // Arrange — seed one table and two sessions (one active, one closed)
    var table = Table.Create(number: 100, code: "T100");
    _db.Tables.Add(table);
    await _db.SaveChangesAsync();

    var closedSession = GuestSession.Create(table.Id);
    closedSession.Close();
    var activeSession = GuestSession.Create(table.Id);
    _db.GuestSessions.AddRange(closedSession, activeSession);
    await _db.SaveChangesAsync();

    var repo = new SpecificationEvaluator();
    var spec = new ActiveSessionByTableIdSpec(table.Id);

    // Act
    var result = await _db.GuestSessions
      .WithSpecification(spec)
      .FirstOrDefaultAsync();

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(activeSession.Id);
    result.Status.Should().Be(GuestSessionStatus.Active);
  }

  [Fact]
  public async Task ActiveSessionByTableIdSpec_WhenNoActiveSession_ShouldReturnNull()
  {
    var table = Table.Create(number: 101, code: "T101");
    _db.Tables.Add(table);
    await _db.SaveChangesAsync();

    var session = GuestSession.Create(table.Id);
    session.Close();
    _db.GuestSessions.Add(session);
    await _db.SaveChangesAsync();

    var spec = new ActiveSessionByTableIdSpec(table.Id);

    var result = await _db.GuestSessions
      .WithSpecification(spec)
      .FirstOrDefaultAsync();

    result.Should().BeNull();
  }
}
