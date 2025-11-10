using AccountService.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AccountService.Services.Impl;

public class EmailService(
    IOptions<EmailSettings> emailSettings,
    ILogger<EmailService> logger,
    IConfiguration configuration)
    : IEmailService
{
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task SendEmailVerificationAsync(string email, string username, string token)
    {
        var baseUrl = configuration["BaseUrl"] ?? "http://localhost:3001";
        var verificationUrl = $"{baseUrl}/api/auth/verify-email?token={token}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background-color: #4F46E5;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to CodeHakam!</h1>
        </div>
        <div class=""content"">
            <h2>Hi {username},</h2>
            <p>Thank you for registering with CodeHakam. Please verify your email address to activate your account.</p>
            <p style=""text-align: center;"">
                <a href=""{verificationUrl}"" class=""button"">Verify Email Address</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #4F46E5;"">{verificationUrl}</p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create an account with CodeHakam, please ignore this email.</p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.UtcNow.Year} CodeHakam. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, "Verify Your Email - CodeHakam", htmlBody);
    }

    public async Task SendPasswordResetAsync(string email, string username, string token)
    {
        var baseUrl = configuration["BaseUrl"] ?? "http://localhost:3001";
        var resetUrl = $"{baseUrl}/reset-password?token={token}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #DC2626; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background-color: #DC2626;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .warning {{ background-color: #FEF3C7; padding: 15px; border-left: 4px solid #F59E0B; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Password Reset Request</h1>
        </div>
        <div class=""content"">
            <h2>Hi {username},</h2>
            <p>We received a request to reset your password for your CodeHakam account.</p>
            <p style=""text-align: center;"">
                <a href=""{resetUrl}"" class=""button"">Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; color: #DC2626;"">{resetUrl}</p>
            <div class=""warning"">
                <strong>Security Notice:</strong> This link will expire in 1 hour. If you didn't request a password reset, please ignore this email and your password will remain unchanged.
            </div>
            <p>For security reasons, all active sessions will be terminated once you reset your password.</p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.UtcNow.Year} CodeHakam. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, "Reset Your Password - CodeHakam", htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string email, string username)
    {
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #10B981; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; }}
        .feature {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Welcome to CodeHakam!</h1>
        </div>
        <div class=""content"">
            <h2>Hi {username},</h2>
            <p>Your email has been verified successfully! You're now ready to start your coding journey with CodeHakam.</p>

            <h3>What's next?</h3>
            <div class=""feature"">
                <strong>📝 Solve Problems:</strong> Browse our extensive problem library and start solving challenges.
            </div>
            <div class=""feature"">
                <strong>🏆 Join Contests:</strong> Participate in competitive programming contests and climb the leaderboard.
            </div>
            <div class=""feature"">
                <strong>📊 Track Progress:</strong> Monitor your statistics, rating, and achievements.
            </div>
            <div class=""feature"">
                <strong>👥 Connect:</strong> Join our community and learn from fellow programmers.
            </div>

            <p style=""margin-top: 30px;"">Happy coding! 💻</p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.UtcNow.Year} CodeHakam. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, "Welcome to CodeHakam - Let's Get Started!", htmlBody);
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Connect to SMTP server
            await client.ConnectAsync(
                _emailSettings.SmtpHost,
                _emailSettings.SmtpPort,
                _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None
            );

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_emailSettings.SmtpUsername) &&
                !string.IsNullOrEmpty(_emailSettings.SmtpPassword))
            {
                await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
            }

            // Send email
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", to);
            throw;
        }
    }
}
