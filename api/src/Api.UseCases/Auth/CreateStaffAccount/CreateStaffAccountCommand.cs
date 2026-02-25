using Api.UseCases.Interfaces;

namespace Api.UseCases.Auth.CreateStaffAccount;

/// <summary>
/// Command to create a staff account with an auto-generated temporary password.
/// Only Admins can execute this command.
/// </summary>
public record CreateStaffAccountCommand(
  string Username,
  string FullName,
  string Role) : ICommand<Result<TemporaryPasswordDto>>;
