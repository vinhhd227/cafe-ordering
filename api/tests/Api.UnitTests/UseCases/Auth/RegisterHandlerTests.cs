using Api.Core.Aggregates.CustomerAggregate;
using Api.UseCases.Auth.Register;
using Api.UseCases.Interfaces;
using Ardalis.SharedKernel;

namespace Api.UnitTests.UseCases.Auth;

public class RegisterHandlerTests
{
  private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
  private readonly IRepository<Customer> _customerRepo = Substitute.For<IRepository<Customer>>();
  private readonly RegisterHandler _handler;

  public RegisterHandlerTests()
  {
    _handler = new RegisterHandler(_customerRepo, _identityService);
  }

  [Fact]
  public async Task Handle_WhenRegistrationSucceeds_ShouldReturnCustomerIdAndEmail()
  {
    _identityService.CreateUserAsync(
        Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), "Customer")
      .Returns(Result.Success(Guid.NewGuid().ToString()));

    var result = await _handler.Handle(
      new RegisterCommand("john.doe", "john@example.com", "Secret@123", "John Doe"), default);

    result.IsSuccess.Should().BeTrue();
    result.Value.Email.Should().Be("john@example.com");
    result.Value.CustomerId.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public async Task Handle_WhenRegistrationSucceeds_ShouldAddCustomerToRepository()
  {
    _identityService.CreateUserAsync(
        Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(Result.Success(Guid.NewGuid().ToString()));

    await _handler.Handle(
      new RegisterCommand("john.doe", "john@example.com", "Secret@123", "John Doe"), default);

    await _customerRepo.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenCreateUserFails_ShouldReturnError()
  {
    _identityService.CreateUserAsync(
        Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(Result<string>.Error("Username is already taken"));

    var result = await _handler.Handle(
      new RegisterCommand("taken-user", "john@example.com", "Secret@123", "John Doe"), default);

    result.IsSuccess.Should().BeFalse();
    result.Status.Should().Be(ResultStatus.Error);
  }

  [Fact]
  public async Task Handle_WhenCreateUserFails_ShouldNotAddCustomerToRepository()
  {
    _identityService.CreateUserAsync(
        Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(Result<string>.Error("Email already registered"));

    await _handler.Handle(
      new RegisterCommand("john.doe", "taken@example.com", "Secret@123", "John Doe"), default);

    await _customerRepo.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ShouldAlwaysAssignCustomerRole()
  {
    _identityService.CreateUserAsync(
        Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(Result<string>.Error("stop early"));

    await _handler.Handle(
      new RegisterCommand("john.doe", "john@example.com", "Secret@123", "John Doe"), default);

    // Role must always be "Customer" regardless of the input
    await _identityService.Received(1).CreateUserAsync(
      Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), "Customer");
  }

  [Fact]
  public async Task Handle_WithSingleWordFullName_ShouldSucceed()
  {
    // Edge case: FullName with no space — firstName = "Madonna", lastName = ""
    _identityService.CreateUserAsync(
        Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(Result.Success(Guid.NewGuid().ToString()));

    var result = await _handler.Handle(
      new RegisterCommand("madonna", "madonna@example.com", "Secret@123", "Madonna"), default);

    result.IsSuccess.Should().BeTrue();
    result.Value.Email.Should().Be("madonna@example.com");
  }
}
