namespace Api.UseCases.Auth.Login;

/// <summary>
/// Identifies which frontend application is initiating the login request.
/// Used to enforce app-level access control based on role permissions.
/// </summary>
public enum AppType
{
    /// <summary>Admin management site — requires <c>admin.access</c> permission.</summary>
    Admin,

    /// <summary>Customer ordering site — requires <c>customer.access</c> permission.</summary>
    Client
}
