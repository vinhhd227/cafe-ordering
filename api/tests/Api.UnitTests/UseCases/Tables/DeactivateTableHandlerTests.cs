using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Tables.Deactivate;

namespace Api.UnitTests.UseCases.Tables;

public class DeactivateTableHandlerTests
{
  private readonly IRepositoryBase<Table> _repo = Substitute.For<IRepositoryBase<Table>>();
  private readonly DeactivateTableHandler _handler;

  public DeactivateTableHandlerTests()
  {
    _handler = new DeactivateTableHandler(_repo);
  }

  [Fact]
  public async Task WhenTableNotFound_ReturnsNotFound()
  {
    _repo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
         .Returns((Table?)null);

    var result = await _handler.Handle(new DeactivateTableCommand(99), default);

    result.Status.Should().Be(ResultStatus.NotFound);
    await _repo.DidNotReceive().UpdateAsync(Arg.Any<Table>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WhenTableIsActive_DeactivatesAndSaves()
  {
    var table = Table.Create("F1-01"); // IsActive = true

    _repo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
         .Returns(table);

    var result = await _handler.Handle(new DeactivateTableCommand(1), default);

    result.IsSuccess.Should().BeTrue();
    table.IsActive.Should().BeFalse();
    await _repo.Received(1).UpdateAsync(table, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WhenTableOccupied_ReturnsConflict()
  {
    var table = Table.Create("F1-01");
    table.OpenSession(Guid.NewGuid()); // Status = Occupied

    _repo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
         .Returns(table);

    var result = await _handler.Handle(new DeactivateTableCommand(1), default);

    result.Status.Should().Be(ResultStatus.Conflict);
    table.IsActive.Should().BeTrue(); // unchanged
    await _repo.DidNotReceive().UpdateAsync(Arg.Any<Table>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WhenTableAlreadyInactive_StillSucceedsIdempotently()
  {
    var table = Table.Create("F1-01");
    table.Deactivate(); // already inactive

    _repo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
         .Returns(table);

    var result = await _handler.Handle(new DeactivateTableCommand(1), default);

    result.IsSuccess.Should().BeTrue();
    table.IsActive.Should().BeFalse();
    await _repo.Received(1).UpdateAsync(table, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WhenTableCleaning_DeactivatesSuccessfully()
  {
    var table = Table.Create("F1-01");
    table.OpenSession(Guid.NewGuid());
    table.CloseSession(); // Status = Cleaning, still deactivatable

    _repo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
         .Returns(table);

    var result = await _handler.Handle(new DeactivateTableCommand(1), default);

    result.IsSuccess.Should().BeTrue();
    table.IsActive.Should().BeFalse();
  }
}
