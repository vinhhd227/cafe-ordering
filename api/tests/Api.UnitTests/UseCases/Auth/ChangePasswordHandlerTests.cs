using Api.Core.Interfaces;
using Api.UseCases.Auth.ChangePassword;
using Api.UseCases.Interfaces;

namespace Api.UnitTests.UseCases.Auth;

public class ChangePasswordHandlerTests
{
  private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
  private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
  private readonly ChangePasswordHandler _handler;

  public ChangePasswordHandlerTests()
  {
    _handler = new ChangePasswordHandler(_identityService, _currentUserService);
  }

  [Fact]
  public async Task Handle_WhenUserIdIsNull_ShouldReturnUnauthorized()
  {
    _currentUserService.UserId.Returns((string?)null);

    var result = await _handler.Handle(
      new ChangePasswordCommand("Current@1", "New@Password1"), default);

    result.Status.Should().Be(ResultStatus.Unauthorized);
    await _identityService.DidNotReceive().ChangePasswordAsync(
      Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
  }

  [Fact]
  public async Task Handle_WhenUserIdIsEmpty_ShouldReturnUnauthorized()
  {
    _currentUserService.UserId.Returns(string.Empty);

    var result = await _handler.Handle(
      new ChangePasswordCommand("Current@1", "New@Password1"), default);

    result.Status.Should().Be(ResultStatus.Unauthorized);
  }

  [Fact]
  public async Task Handle_WhenUserIdIsNotAValidGuid_ShouldReturnUnauthorized()
  {
    _currentUserService.UserId.Returns("not-a-valid-guid");

    var result = await _handler.Handle(
      new ChangePasswordCommand("Current@1", "New@Password1"), default);

    result.Status.Should().Be(ResultStatus.Unauthorized);
    await _identityService.DidNotReceive().ChangePasswordAsync(
      Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
  }

  [Fact]
  public async Task Handle_WhenPasswordChangeSucceeds_ShouldReturnSuccess()
  {
    var userId = Guid.NewGuid();
    _currentUserService.UserId.Returns(userId.ToString());
    _identityService.ChangePasswordAsync(userId, "Current@1", "New@Password1")
                    .Returns(Result.Success());

    var result = await _handler.Handle(
      new ChangePasswordCommand("Current@1", "New@Password1"), default);

    result.IsSuccess.Should().BeTrue();
  }

  [Fact]
  public async Task Handle_WhenCurrentPasswordIsWrong_ShouldReturnInvalid()
  {
    var userId = Guid.NewGuid();
    _currentUserService.UserId.Returns(userId.ToString());
    _identityService.ChangePasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Result.Invalid(new ValidationError("password", "Incorrect password")));

    var result = await _handler.Handle(
      new ChangePasswordCommand("WrongCurrent@1", "New@Password1"), default);

    result.Status.Should().Be(ResultStatus.Invalid);
  }

  [Fact]
  public async Task Handle_ShouldForwardCorrectUserIdAndPasswordsToIdentityService()
  {
    var userId = Guid.NewGuid();
    _currentUserService.UserId.Returns(userId.ToString());
    _identityService.ChangePasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Result.Success());

    await _handler.Handle(new ChangePasswordCommand("Current@1", "New@Password1"), default);

    await _identityService.Received(1)
      .ChangePasswordAsync(userId, "Current@1", "New@Password1");
  }
}
