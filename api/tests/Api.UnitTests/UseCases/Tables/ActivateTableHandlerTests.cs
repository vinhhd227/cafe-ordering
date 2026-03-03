using Api.Core.Aggregates.TableAggregate;
using Api.Core.Aggregates.TableAggregate.Specifications;
using Api.UseCases.Tables.Activate;

namespace Api.UnitTests.UseCases.Tables;

public class ActivateTableHandlerTests
{
  private readonly IRepositoryBase<Table> _repo = Substitute.For<IRepositoryBase<Table>>();
  private readonly ActivateTableHandler _handler;

  public ActivateTableHandlerTests()
  {
    _handler = new ActivateTableHandler(_repo);
  }

  [Fact]
  public async Task WhenTableNotFound_ReturnsNotFound()
  {
    _repo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
         .Returns((Table?)null);

    var result = await _handler.Handle(new ActivateTableCommand(99), default);

    result.Status.Should().Be(ResultStatus.NotFound);
    await _repo.DidNotReceive().UpdateAsync(Arg.Any<Table>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WhenTableIsInactive_ActivatesAndSaves()
  {
    var table = Table.Create("F1-01");
    table.Deactivate();

    _repo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
         .Returns(table);

    var result = await _handler.Handle(new ActivateTableCommand(1), default);

    result.IsSuccess.Should().BeTrue();
    table.IsActive.Should().BeTrue();
    await _repo.Received(1).UpdateAsync(table, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WhenTableAlreadyActive_StillSucceedsIdempotently()
  {
    var table = Table.Create("F1-01"); // IsActive = true by default

    _repo.FirstOrDefaultAsync(Arg.Any<TableByIdSpec>(), Arg.Any<CancellationToken>())
         .Returns(table);

    var result = await _handler.Handle(new ActivateTableCommand(1), default);

    result.IsSuccess.Should().BeTrue();
    table.IsActive.Should().BeTrue();
    await _repo.Received(1).UpdateAsync(table, Arg.Any<CancellationToken>());
  }
}
