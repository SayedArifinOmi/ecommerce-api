namespace AuthApi.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ResetCodeHash { get; set; }
    public DateTime? ResetCodeExpiry { get; set; }
    //public string FullName { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    //public DateTime PasswordResetTokenExpiry { get; internal set; }
    //public string? PasswordResetToken { get; set; }


}
