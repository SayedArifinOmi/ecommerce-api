using System.ComponentModel.DataAnnotations;
namespace AuthApi.Contracts;
public record ProfileUpdateDto([Required] string Username , string Email);
public record ChangePasswordDto([Required] string CurrentPassword, [Required, MinLength(6)] string NewPassword);
