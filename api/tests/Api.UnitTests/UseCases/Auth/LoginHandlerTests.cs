using Api.UseCases.Auth.Login;
using Api.UseCases.Interfaces;

namespace Api.UnitTests.UseCases.Auth;

public class LoginHandlerTests
{
  private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
  private readonly LoginHandler _handler;

  public LoginHandlerTests()
  {
    _handler = new LoginHandler(_identityService);
  }

  [Fact]
  public async Task Handle_WhenCredentialsAreValid_ShouldReturnTokens()
  {
    var authDto = new AuthResponseDto("access-token", "refresh-token", DateTime.UtcNow.AddDays(7));
    _identityService.LoginAsync("john.doe", "Password@123")
                    .Returns(Result.Success(authDto));

    var result = await _handler.Handle(new LoginCommand("john.doe", "Password@123"), default);

    result.IsSuccess.Should().BeTrue();
    result.Value.AccessToken.Should().Be("access-token");
    result.Value.RefreshToken.Should().Be("refresh-token");
  }

  [Fact]
  public async Task Handle_WhenCredentialsAreInvalid_ShouldReturnUnauthorized()
  {
    _identityService.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Result<AuthResponseDto>.Unauthorized());

    var result = await _handler.Handle(new LoginCommand("john.doe", "wrong-password"), default);

    result.Status.Should().Be(ResultStatus.Unauthorized);
  }

  [Fact]
  public async Task Handle_WhenAccountIsDeactivated_ShouldReturnUnauthorized()
  {
    _identityService.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Result<AuthResponseDto>.Unauthorized());

    var result = await _handler.Handle(new LoginCommand("deactivated-user", "Password@123"), default);

    result.IsSuccess.Should().BeFalse();
    result.Status.Should().Be(ResultStatus.Unauthorized);
  }

  [Fact]
  public async Task Handle_ShouldForwardUsernameAndPasswordToIdentityService()
  {
    _identityService.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Result<AuthResponseDto>.Unauthorized());

    await _handler.Handle(new LoginCommand("john.doe", "Secret@123"), default);

    await _identityService.Received(1).LoginAsync("john.doe", "Secret@123");
  }
}
