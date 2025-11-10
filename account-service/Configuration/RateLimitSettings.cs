namespace AccountService.Configuration;

public class RateLimitSettings
{
    public int LoginAttempts { get; set; } = 5;
    public int LoginWindowMinutes { get; set; } = 15;
    public int RegisterAttempts { get; set; } = 3;
    public int RegisterWindowHours { get; set; } = 1;
    public int PasswordResetAttempts { get; set; } = 3;
    public int PasswordResetWindowHours { get; set; } = 1;
}
