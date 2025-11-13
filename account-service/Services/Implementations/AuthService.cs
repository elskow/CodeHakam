using AccountService.Constants;
using AccountService.Data;
using AccountService.DTOs;
using AccountService.DTOs.Common;
using AccountService.Events;
using AccountService.Models;
using AccountService.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Services.Implementations;

public sealed class AuthService(
    ApplicationDbContext context,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ITokenService tokenService,
    IEmailService emailService,
    IEventPublisher eventPublisher,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<(bool Success, RegisterResponse? Response, string? Error)> RegisterAsync(
        RegisterRequest request,
        string? ipAddress = null)
    {
        try
        {
            // Check if email already exists
            if (await userManager.FindByEmailAsync(request.Email) != null)
            {
                return (false, null, "Email is already registered");
            }

            // Check if username already exists
            if (await userManager.FindByNameAsync(request.Username) != null)
            {
                return (false, null, "Username is already taken");
            }

            // Create user
            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                Rating = ApplicationConstants.Defaults.UserRating
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, null, errors);
            }

            // Assign default role
            var defaultRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == ApplicationConstants.Roles.DefaultRole);
            if (defaultRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id,
                    AssignedAt = DateTime.UtcNow
                };
                context.UserRoles.Add(userRole);
            }

            // Create default statistics and settings
            context.UserStatistics.Add(new UserStatistics { UserId = user.Id });
            context.UserSettings.Add(new UserSettings { UserId = user.Id });

            await context.SaveChangesAsync();

            // Generate email verification token
            var verificationToken = await tokenService.GenerateEmailVerificationTokenAsync();
            user.VerificationToken = tokenService.HashToken(verificationToken);
            user.VerificationTokenExpiry = DateTime.UtcNow.AddHours(ApplicationConstants.TokenExpiry.EmailVerificationHours);
            await userManager.UpdateAsync(user);

            // Send verification email (non-blocking - don't fail registration if email fails)
            try
            {
                await emailService.SendEmailVerificationAsync(user.Email!, user.UserName!, verificationToken);
            }
            catch (Exception emailEx)
            {
                logger.LogWarning(emailEx, "Failed to send verification email to {Email}. User can request resend later.", user.Email);
            }

            // Publish events to outbox (must succeed for registration to complete)
            try
            {
                await eventPublisher.PublishUserRegisteredAsync(new UserRegisteredEvent
                {
                    UserId = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Timestamp = DateTime.UtcNow
                });

                // Publish user.created for cross-service data sync
                await eventPublisher.PublishUserCreatedAsync(new UserCreatedEvent
                {
                    UserId = user.Id,
                    Username = user.UserName ?? string.Empty,
                    DisplayName = user.FullName ?? user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    AvatarUrl = user.AvatarUrl,
                    CreatedAt = user.CreatedAt
                });

                // Save outbox events to database
                await context.SaveChangesAsync();
                logger.LogInformation("Outbox events saved for user {Username}", user.UserName);
            }
            catch (Exception eventEx)
            {
                logger.LogError(eventEx, "Failed to save outbox events for {Username}", user.UserName);
                // Roll back user creation if event publishing fails
                await userManager.DeleteAsync(user);
                return (false, null, "Registration failed - could not queue notification events");
            }

            logger.LogInformation("User {Username} registered successfully", user.UserName);

            var response = new RegisterResponse
            {
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return (false, null, "An error occurred during registration");
        }
    }

    public async Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(
        LoginRequest request,
        string? ipAddress = null)
    {
        try
        {
            var user = await userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return (false, null, "Invalid email or password");
            }

            // Check if user is banned
            if (user.IsBanned)
            {
                return (false, null, $"Account is banned. Reason: {user.BanReason}");
            }

            // Verify password
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return (false, null, "Account is locked due to multiple failed login attempts");
                }

                return (false, null, "Invalid email or password");
            }

            // Check email verification (optional - can be made strict)
            // if (!user.IsVerified)
            // {
            //     return (false, null, "Please verify your email before logging in");
            // }

            // Get user roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Generate tokens
            var accessToken = await tokenService.GenerateAccessTokenAsync(user, roles);
            var refreshToken = await tokenService.GenerateRefreshTokenAsync();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(ApplicationConstants.TokenExpiry.RefreshTokenDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
            context.RefreshTokens.Add(refreshTokenEntity);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;

            // Publish event to outbox
            try
            {
                await eventPublisher.PublishUserLoggedInAsync(new UserLoggedInEvent
                {
                    UserId = user.Id,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                });

                // Save login updates and outbox events atomically
                await context.SaveChangesAsync();
            }
            catch (Exception eventEx)
            {
                logger.LogError(eventEx, "Failed to save login or outbox events for {Username}", user.UserName);
                throw;
            }

            logger.LogInformation("User {Username} logged in successfully", user.UserName);

            return (true, new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = ApplicationConstants.Defaults.TokenExpirySeconds,
                User = new UserAuthDto
                {
                    Id = user.Id,
                    Username = user.UserName!,
                    Email = user.Email!,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio,
                    Country = user.Country,
                    Organization = user.Organization,
                    Rating = user.Rating,
                    IsVerified = user.IsVerified,
                    IsBanned = user.IsBanned,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Roles = roles
                }
            }, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for {Email}", request.Email);
            return (false, null, "An error occurred during login");
        }
    }

    public async Task<(bool Success, RefreshTokenResponse? Response, string? Error)> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? ipAddress = null)
    {
        try
        {
            var tokenHash = tokenService.HashToken(request.RefreshToken);
            var refreshToken = await context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                return (false, null, "Invalid or expired refresh token");
            }

            var user = refreshToken.User;

            // Check if user is banned
            if (user.IsBanned)
            {
                return (false, null, "Account is banned");
            }

            // Get user roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Generate new tokens
            var newAccessToken = await tokenService.GenerateAccessTokenAsync(user, roles);
            var newRefreshToken = await tokenService.GenerateRefreshTokenAsync();

            // Revoke old refresh token and create new one
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = tokenService.HashToken(newRefreshToken);

            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenService.HashToken(newRefreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(ApplicationConstants.TokenExpiry.RefreshTokenDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
            context.RefreshTokens.Add(newRefreshTokenEntity);

            await context.SaveChangesAsync();

            var response = new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = ApplicationConstants.Defaults.TokenExpirySeconds
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh");
            return (false, null, "An error occurred during token refresh");
        }
    }

    public async Task<(bool Success, string? Error)> LogoutAsync(
        long userId,
        string refreshToken,
        string? ipAddress = null)
    {
        try
        {
            var tokenHash = tokenService.HashToken(refreshToken);
            var token = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.TokenHash == tokenHash);

            if (token is null)
            {
                return (false, "Invalid refresh token");
            }

            if (!token.IsActive)
            {
                return (false, "Refresh token is already revoked or expired");
            }

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            await context.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return (false, "An error occurred during logout");
        }
    }

    public async Task<(bool Success, string? Error)> VerifyEmailAsync(string token)
    {
        try
        {
            var tokenHash = tokenService.HashToken(token);
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.VerificationToken == tokenHash);

            if (user == null)
            {
                return (false, "Invalid verification token");
            }

            if (user.VerificationTokenExpiry < DateTime.UtcNow)
            {
                return (false, "Verification token has expired");
            }

            user.IsVerified = true;
            user.EmailConfirmed = true;
            user.VerificationToken = null;
            user.VerificationTokenExpiry = null;

            await userManager.UpdateAsync(user);

            logger.LogInformation("Email verified for user {Username}", user.UserName);

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during email verification");
            return (false, "An error occurred during email verification");
        }
    }

    public async Task<(bool Success, string? Error)> ForgotPasswordAsync(string email)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return (true, null);
            }

            // Generate password reset token
            var resetToken = await tokenService.GeneratePasswordResetTokenAsync();
            user.PasswordResetToken = tokenService.HashToken(resetToken);
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(ApplicationConstants.TokenExpiry.PasswordResetHours);
            await userManager.UpdateAsync(user);

            // Send password reset email (non-blocking)
            try
            {
                await emailService.SendPasswordResetAsync(user.Email!, user.UserName!, resetToken);
            }
            catch (Exception emailEx)
            {
                logger.LogWarning(emailEx, "Failed to send password reset email to {Email}", user.Email);
            }

            // Publish event (non-blocking)
            try
            {
                await eventPublisher.PublishPasswordResetRequestedAsync(new PasswordResetRequestedEvent
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception eventEx)
            {
                logger.LogWarning(eventEx, "Failed to publish PasswordResetRequested event for {Username}", user.UserName);
            }

            logger.LogInformation("Password reset requested for user {Username}", user.UserName);

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during forgot password for {Email}", email);
            return (false, "An error occurred while processing your request");
        }
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var tokenHash = tokenService.HashToken(request.Token);
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.PasswordResetToken == tokenHash);

            if (user == null)
            {
                return (false, "Invalid password reset token");
            }

            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return (false, "Password reset token has expired");
            }

            // Reset password
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            // Clear reset token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            await userManager.UpdateAsync(user);

            // Revoke all refresh tokens for security
            var refreshTokens = await context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var rt in refreshTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Password reset successfully for user {Username}", user.UserName);

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during password reset");
            return (false, "An error occurred during password reset");
        }
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return (false, "User not found");
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            logger.LogInformation("Password changed successfully for user {Username}", user.UserName);

            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during password change for user {UserId}", userId);
            return (false, "An error occurred while changing password");
        }
    }
}
