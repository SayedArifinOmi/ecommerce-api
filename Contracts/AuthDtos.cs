namespace AuthApi.Contracts;

public record RegisterDto(string Username, string Email, string Password, string Role);
public record LoginDto(string Email, string Password);
public record TokenResponse(string AccessToken, string RefreshToken);
public record RefreshRequest(string RefreshToken);
public class ForgotPasswordDto{public string Email { get; set; } = "";}
public class ResetPasswordDto
{
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

