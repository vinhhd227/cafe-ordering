using Api.UseCases.Auth.RefreshToken;
using Api.UseCases.Interfaces;

namespace Api.UnitTests.UseCases.Auth;

public class RefreshTokenHandlerTests
{
  private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
  private readonly RefreshTokenHandler _handler;

  public RefreshTokenHandlerTests()
  {
    _handler = new RefreshTokenHandler(_identityService);
  }

  [Fact]
  public async Task Handle_WhenTokenIsValid_ShouldReturnNewTokens()
  {
    var authDto = new AuthResponseDto("new-access-token", "new-refresh-token", DateTime.UtcNow.AddDays(7));
    _identityService.RefreshTokenAsync("valid-refresh-token")
                    .Returns(Result.Success(authDto));

    var result = await _handler.Handle(new RefreshTokenCommand("valid-refresh-token"), default);

    result.IsSuccess.Should().BeTrue();
    result.Value.AccessToken.Should().Be("new-access-token");
    result.Value.RefreshToken.Should().Be("new-refresh-token");
  }

  [Fact]
  public async Task Handle_WhenTokenIsExpired_ShouldReturnUnauthorized()
  {
    _identityService.RefreshTokenAsync(Arg.Any<string>())
                    .Returns(Result<AuthResponseDto>.Unauthorized());

    var result = await _handler.Handle(new RefreshTokenCommand("expired-token"), default);

    result.Status.Should().Be(ResultStatus.Unauthorized);
  }

  [Fact]
  public async Task Handle_WhenTokenIsRevoked_ShouldReturnUnauthorized()
  {
    // Revoked tokens are treated as suspicious — all user tokens are revoked and Unauthorized returned
    _identityService.RefreshTokenAsync(Arg.Any<string>())
                    .Returns(Result<AuthResponseDto>.Unauthorized());

    var result = await _handler.Handle(new RefreshTokenCommand("revoked-token"), default);

    result.IsSuccess.Should().BeFalse();
    result.Status.Should().Be(ResultStatus.Unauthorized);
  }

  [Fact]
  public async Task Handle_ShouldForwardRefreshTokenToIdentityService()
  {
    _identityService.RefreshTokenAsync(Arg.Any<string>())
                    .Returns(Result<AuthResponseDto>.Unauthorized());

    await _handler.Handle(new RefreshTokenCommand("my-refresh-token"), default);

    await _identityService.Received(1).RefreshTokenAsync("my-refresh-token");
  }
}
