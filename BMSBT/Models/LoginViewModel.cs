namespace BMSBT.Models
{
    public class LoginViewModel
    {
    
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime? LastLoginTime { get; set; } // Nullable in case no login time is set
    }
}
