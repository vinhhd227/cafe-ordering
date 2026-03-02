using Api.Core.Aggregates.TableAggregate;

namespace Api.UnitTests.Aggregates;

public class TableTests
{
  [Fact]
  public void Create_ShouldSetAvailableStatusAndActiveFlag()
  {
    var table = Table.Create(number: 5, code: "T05");

    table.Number.Should().Be(5);
    table.Code.Should().Be("T05");
    table.Status.Should().Be(TableStatus.Available);
    table.IsActive.Should().BeTrue();
    table.ActiveSessionId.Should().BeNull();
  }

  [Fact]
  public void OpenSession_ShouldSetOccupiedAndActiveSessionId()
  {
    var table = Table.Create(1, "T01");
    var sessionId = Guid.NewGuid();

    table.OpenSession(sessionId);

    table.Status.Should().Be(TableStatus.Occupied);
    table.ActiveSessionId.Should().Be(sessionId);
  }

  [Fact]
  public void OpenSession_WhenAlreadyOccupied_ShouldThrow()
  {
    var table = Table.Create(1, "T01");
    table.OpenSession(Guid.NewGuid());

    var act = () => table.OpenSession(Guid.NewGuid());

    act.Should().Throw<InvalidOperationException>()
       .WithMessage("*already has an active session*");
  }

  [Fact]
  public void CloseSession_ShouldSetCleaningAndClearSessionId()
  {
    var table = Table.Create(1, "T01");
    table.OpenSession(Guid.NewGuid());

    table.CloseSession();

    table.Status.Should().Be(TableStatus.Cleaning);
    table.ActiveSessionId.Should().BeNull();
  }

  [Fact]
  public void MarkAvailable_ShouldSetAvailableStatus()
  {
    var table = Table.Create(1, "T01");
    table.OpenSession(Guid.NewGuid());
    table.CloseSession();

    table.MarkAvailable();

    table.Status.Should().Be(TableStatus.Available);
  }

  [Fact]
  public void Deactivate_ShouldSetIsActiveFalse()
  {
    var table = Table.Create(1, "T01");

    table.Deactivate();

    table.IsActive.Should().BeFalse();
  }
}
