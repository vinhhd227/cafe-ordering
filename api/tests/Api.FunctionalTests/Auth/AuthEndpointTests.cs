using System.Net;
using Api.Web.Endpoints.Auth;

namespace Api.FunctionalTests.Auth;

[Collection(nameof(FunctionalTestCollection))]
public class AuthEndpointTests
{
  private readonly ApiFactory _factory;
  private readonly HttpClient _client;
  private readonly ITestOutputHelper _output;

  public AuthEndpointTests(ApiFactory factory, ITestOutputHelper output)
  {
    _factory = factory;
    _client  = factory.CreateClient();
    _output  = output;
  }

  // ──────────────────────────── POST /api/auth/login ────────────────────────────

  [Fact]
  public async Task Login_WithValidCredentials_Returns200WithTokens()
  {
    var result = await _client.PostAndDeserializeAsync<LoginResponse>(
      "/api/auth/login",
      JsonContent.Create(new { Username = "admin", Password = "Admin@123456" }),
      _output);

    result.Success.Should().BeTrue();
    result.AccessToken.Should().NotBeNullOrEmpty();
    result.RefreshToken.Should().NotBeNullOrEmpty();
    result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
  }

  [Fact]
  public async Task Login_WithInvalidPassword_Returns401()
  {
    var response = await _client.PostAsync(
      "/api/auth/login",
      JsonContent.Create(new { Username = "admin", Password = "wrong-password" }));

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Login_WithNonExistentUsername_Returns401()
  {
    var response = await _client.PostAsync(
      "/api/auth/login",
      JsonContent.Create(new { Username = "nobody-here", Password = "Password@123" }));

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Login_WithEmptyUsernameAndPassword_Returns400()
  {
    var response = await _client.PostAsync(
      "/api/auth/login",
      JsonContent.Create(new { Username = "", Password = "" }));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  // ──────────────────────────── POST /api/auth/register ────────────────────────────

  [Fact]
  public async Task Register_WithValidData_Returns200WithCustomerId()
  {
    var username = $"newuser-{Guid.NewGuid():N}";

    var result = await _client.PostAndDeserializeAsync<RegisterResponse>(
      "/api/auth/register",
      JsonContent.Create(new
      {
        Username = username,
        Email    = $"{username}@example.com",
        Password = "Secret@123",
        FullName = "Test User"
      }),
      _output);

    result.CustomerId.Should().NotBeNullOrEmpty();
    result.Email.Should().Be($"{username}@example.com");
  }

  [Fact]
  public async Task Register_WithDuplicateUsername_Returns400()
  {
    // "admin" is seeded — registering with the same username must fail
    await _client.PostAndEnsureBadRequestAsync(
      "/api/auth/register",
      JsonContent.Create(new
      {
        Username = "admin",
        Email    = "another-admin@example.com",
        Password = "Secret@123",
        FullName = "Duplicate Admin"
      }),
      _output);
  }

  [Fact]
  public async Task Register_WithWeakPassword_Returns400()
  {
    var username = $"weakpass-{Guid.NewGuid():N}";

    await _client.PostAndEnsureBadRequestAsync(
      "/api/auth/register",
      JsonContent.Create(new
      {
        Username = username,
        Email    = $"{username}@example.com",
        Password = "weak",          // too short, no special chars
        FullName = "Weak Pass User"
      }),
      _output);
  }

  // ──────────────────────────── POST /api/auth/refresh ────────────────────────────

  [Fact]
  public async Task RefreshToken_WithValidToken_Returns200WithRotatedTokens()
  {
    // Step 1: Login to obtain a real refresh token
    var loginResponse = await _client.PostAndDeserializeAsync<LoginResponse>(
      "/api/auth/login",
      JsonContent.Create(new { Username = "admin", Password = "Admin@123456" }),
      _output);

    // Step 2: Use the refresh token
    var result = await _client.PostAndDeserializeAsync<RefreshTokenResponse>(
      "/api/auth/refresh",
      JsonContent.Create(new { RefreshToken = loginResponse.RefreshToken }),
      _output);

    result.Success.Should().BeTrue();
    result.AccessToken.Should().NotBeNullOrEmpty();
    result.RefreshToken.Should().NotBeNullOrEmpty();
    // Token rotation: a new refresh token must be issued
    result.RefreshToken.Should().NotBe(loginResponse.RefreshToken);
  }

  [Fact]
  public async Task RefreshToken_WithInvalidToken_Returns401()
  {
    var response = await _client.PostAsync(
      "/api/auth/refresh",
      JsonContent.Create(new { RefreshToken = "completely-invalid-token" }));

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task RefreshToken_WithEmptyToken_Returns400()
  {
    var response = await _client.PostAsync(
      "/api/auth/refresh",
      JsonContent.Create(new { RefreshToken = "" }));

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  // ──────────────────────────── POST /api/auth/check-username ────────────────────────────

  [Fact]
  public async Task CheckUsername_WhenUsernameIsTaken_ReturnsExistsTrue()
  {
    // "admin" is seeded and already registered
    var result = await _client.PostAndDeserializeAsync<CheckUsernameResponse>(
      "/api/auth/check-username",
      JsonContent.Create(new { Username = "admin" }),
      _output);

    result.Exists.Should().BeTrue();
  }

  [Fact]
  public async Task CheckUsername_WhenUsernameIsAvailable_ReturnsExistsFalse()
  {
    var username = $"available-{Guid.NewGuid():N}";

    var result = await _client.PostAndDeserializeAsync<CheckUsernameResponse>(
      "/api/auth/check-username",
      JsonContent.Create(new { Username = username }),
      _output);

    result.Exists.Should().BeFalse();
  }

  [Fact]
  public async Task CheckUsername_IsAccessibleWithoutAuthentication()
  {
    // Must be anonymous — no auth header on _client
    var response = await _client.PostAsync(
      "/api/auth/check-username",
      JsonContent.Create(new { Username = "some-user" }));

    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
  }

  // ──────────────────────────── POST /api/auth/change-password ────────────────────────────

  [Fact]
  public async Task ChangePassword_WithoutAuth_Returns401()
  {
    var response = await _client.PostAsync(
      "/api/auth/change-password",
      JsonContent.Create(new { CurrentPassword = "Admin@123456", NewPassword = "NewPass@456" }));

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task ChangePassword_WithWrongCurrentPassword_Returns400()
  {
    // Login as admin to obtain a real access token
    var loginResponse = await _client.PostAndDeserializeAsync<LoginResponse>(
      "/api/auth/login",
      JsonContent.Create(new { Username = "admin", Password = "Admin@123456" }),
      _output);

    var authClient = _factory.CreateClient();
    authClient.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", loginResponse.AccessToken!);

    await authClient.PostAndEnsureBadRequestAsync(
      "/api/auth/change-password",
      JsonContent.Create(new { CurrentPassword = "WrongPassword@999", NewPassword = "NewPass@456" }),
      _output);
  }

  [Fact]
  public async Task ChangePassword_WithValidCredentials_Returns204()
  {
    // Register a fresh user to avoid side effects on the seeded admin account
    var username = $"chgpwd-{Guid.NewGuid():N}";
    const string originalPassword = "Original@123";
    const string newPassword      = "Changed@456!";

    await _client.PostAsync(
      "/api/auth/register",
      JsonContent.Create(new
      {
        Username = username,
        Email    = $"{username}@example.com",
        Password = originalPassword,
        FullName = "Change Pwd User"
      }));

    // Login as the new user
    var loginResponse = await _client.PostAndDeserializeAsync<LoginResponse>(
      "/api/auth/login",
      JsonContent.Create(new { Username = username, Password = originalPassword }),
      _output);

    var authClient = _factory.CreateClient();
    authClient.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", loginResponse.AccessToken!);

    // Change the password
    var response = await authClient.PostAsync(
      "/api/auth/change-password",
      JsonContent.Create(new { CurrentPassword = originalPassword, NewPassword = newPassword }));

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
  }

  [Fact]
  public async Task ChangePassword_AfterSuccessfulChange_OldPasswordIsNoLongerValid()
  {
    var username = $"chgpwd2-{Guid.NewGuid():N}";
    const string originalPassword = "Original@123";
    const string newPassword      = "Changed@789!";

    await _client.PostAsync(
      "/api/auth/register",
      JsonContent.Create(new
      {
        Username = username,
        Email    = $"{username}@example.com",
        Password = originalPassword,
        FullName = "Change Pwd User 2"
      }));

    var loginResponse = await _client.PostAndDeserializeAsync<LoginResponse>(
      "/api/auth/login",
      JsonContent.Create(new { Username = username, Password = originalPassword }),
      _output);

    var authClient = _factory.CreateClient();
    authClient.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", loginResponse.AccessToken!);

    // Change password
    await authClient.PostAsync(
      "/api/auth/change-password",
      JsonContent.Create(new { CurrentPassword = originalPassword, NewPassword = newPassword }));

    // Attempt login with old password — must fail
    var oldPasswordLogin = await _client.PostAsync(
      "/api/auth/login",
      JsonContent.Create(new { Username = username, Password = originalPassword }));

    oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }
}
