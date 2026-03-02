using Api.Core.Aggregates.GuestSessionAggregate;

namespace Api.UnitTests.Aggregates;

public class GuestSessionTests
{
  [Fact]
  public void Create_ShouldSetActiveStatusAndTableId()
  {
    var session = GuestSession.Create(tableId: 5);

    session.Status.Should().Be(GuestSessionStatus.Active);
    session.TableId.Should().Be(5);
    session.OpenedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    session.ClosedAt.Should().BeNull();
  }

  [Fact]
  public void Create_ShouldRegisterSessionOpenedDomainEvent()
  {
    var session = GuestSession.Create(tableId: 5);

    session.DomainEvents.Should().ContainSingle();
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Create_WithInvalidTableId_ShouldThrow(int tableId)
  {
    var act = () => GuestSession.Create(tableId);

    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void CreateCounter_ShouldHaveNullTableId()
  {
    var session = GuestSession.CreateCounter();

    session.TableId.Should().BeNull();
    session.Status.Should().Be(GuestSessionStatus.Active);
  }

  [Fact]
  public void Close_ShouldSetStatusClosedAndClosedAt()
  {
    var session = GuestSession.Create(tableId: 3);

    session.Close();

    session.Status.Should().Be(GuestSessionStatus.Closed);
    session.ClosedAt.Should().NotBeNull();
    session.ClosedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
  }

  [Fact]
  public void Close_WhenAlreadyClosed_ShouldThrow()
  {
    var session = GuestSession.Create(tableId: 3);
    session.Close();

    var act = () => session.Close();

    act.Should().Throw<InvalidOperationException>()
       .WithMessage("Session is already closed.");
  }

  [Fact]
  public void MergeWithCustomer_ShouldSetCustomerId()
  {
    var session = GuestSession.Create(tableId: 2);

    session.MergeWithCustomer("customer-abc");

    session.CustomerId.Should().Be("customer-abc");
  }

  [Fact]
  public void MergeWithCustomer_WhenClosed_ShouldThrow()
  {
    var session = GuestSession.Create(tableId: 2);
    session.Close();

    var act = () => session.MergeWithCustomer("customer-abc");

    act.Should().Throw<InvalidOperationException>()
       .WithMessage("Cannot merge a closed session.");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  public void MergeWithCustomer_WithEmptyCustomerId_ShouldThrow(string? customerId)
  {
    var session = GuestSession.Create(tableId: 2);

    var act = () => session.MergeWithCustomer(customerId!);

    act.Should().Throw<ArgumentException>();
  }
}
