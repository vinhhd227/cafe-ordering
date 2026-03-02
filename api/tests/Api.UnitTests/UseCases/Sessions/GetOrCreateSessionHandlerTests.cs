using Api.Core.Aggregates.GuestSessionAggregate;
using Api.Core.Aggregates.GuestSessionAggregate.Specifications;
using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Sessions.GetOrCreate;

namespace Api.UnitTests.UseCases.Sessions;

public class GetOrCreateSessionHandlerTests
{
  private readonly IRepositoryBase<Table> _tableRepo = Substitute.For<IRepositoryBase<Table>>();
  private readonly IRepositoryBase<GuestSession> _sessionRepo = Substitute.For<IRepositoryBase<GuestSession>>();
  private readonly GetOrCreateSessionHandler _handler;

  public GetOrCreateSessionHandlerTests()
  {
    _handler = new GetOrCreateSessionHandler(_tableRepo, _sessionRepo);
  }

  [Fact]
  public async Task Handle_WhenTableNotFound_ShouldReturnNotFound()
  {
    _tableRepo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
              .Returns((Table?)null);

    var result = await _handler.Handle(new GetOrCreateSessionCommand(99), default);

    result.Status.Should().Be(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_WhenTableInactive_ShouldReturnError()
  {
    var table = Table.Create(1, "T01");
    table.Deactivate();
    _tableRepo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(table);

    var result = await _handler.Handle(new GetOrCreateSessionCommand(1), default);

    result.Status.Should().Be(ResultStatus.Error);
  }

  [Fact]
  public async Task Handle_WhenActiveSessionExists_ShouldReturnExistingSession()
  {
    var table = Table.Create(1, "T01");
    var existingSession = GuestSession.Create(tableId: 1);

    _tableRepo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(table);
    _sessionRepo.FirstOrDefaultAsync(Arg.Any<ActiveSessionByTableIdSpec>(), Arg.Any<CancellationToken>())
                .Returns(existingSession);

    var result = await _handler.Handle(new GetOrCreateSessionCommand(1), default);

    result.IsSuccess.Should().BeTrue();
    result.Value.SessionId.Should().Be(existingSession.Id);

    // Should NOT create a new session
    await _sessionRepo.DidNotReceive().AddAsync(Arg.Any<GuestSession>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoActiveSession_ShouldCreateNewSession()
  {
    var table = Table.Create(1, "T01");

    _tableRepo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(table);
    _sessionRepo.FirstOrDefaultAsync(Arg.Any<ActiveSessionByTableIdSpec>(), Arg.Any<CancellationToken>())
                .Returns((GuestSession?)null);

    var result = await _handler.Handle(new GetOrCreateSessionCommand(1), default);

    result.IsSuccess.Should().BeTrue();
    result.Value.TableId.Should().Be(1);

    await _sessionRepo.Received(1).AddAsync(Arg.Any<GuestSession>(), Arg.Any<CancellationToken>());
    await _tableRepo.Received(1).UpdateAsync(Arg.Any<Table>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenTableOccupiedButNoActiveSession_ShouldResetTableAndCreateSession()
  {
    var table = Table.Create(1, "T01");
    table.OpenSession(Guid.NewGuid()); // stale Occupied state

    _tableRepo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
              .Returns(table);
    _sessionRepo.FirstOrDefaultAsync(Arg.Any<ActiveSessionByTableIdSpec>(), Arg.Any<CancellationToken>())
                .Returns((GuestSession?)null);

    var result = await _handler.Handle(new GetOrCreateSessionCommand(1), default);

    result.IsSuccess.Should().BeTrue();
    table.Status.Should().Be(TableStatus.Occupied); // re-opened
    table.ActiveSessionId.Should().NotBeNull();
  }
}
