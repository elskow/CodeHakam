using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AccountService.Data;
using AccountService.DTOs;
using AccountService.Models;
using AccountService.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AccountService.Tests.Integration.Controllers;

public class AuthControllerTests : IClassFixture<AccountServiceFactory>
{
    private readonly AccountServiceFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(AccountServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn201Created()
    {
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FullName = "New User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be("newuser");
        result.Data.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturn409Conflict()
    {
        var existingUser = await _factory.CreateTestUserAsync("existing", "existing@example.com");

        var request = new RegisterRequest
        {
            Username = "newusername",
            Email = "existing@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FullName = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("already");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturn400BadRequest()
    {
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "not-an-email",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FullName = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ShouldReturn400BadRequest()
    {
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "DifferentPass123!",
            FullName = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200AndTokens()
    {
        var password = "ValidPass123!";
        var user = await _factory.CreateTestUserAsync("loginuser", "loginuser@example.com", password);

        var request = new LoginRequest
        {
            Email = "loginuser@example.com",
            Password = password,
            RememberMe = false
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.TokenType.Should().Be("Bearer");
        result.Data.ExpiresIn.Should().BeGreaterThan(0);
        result.Data.User.Should().NotBeNull();
        result.Data.User.Username.Should().Be("loginuser");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401Unauthorized()
    {
        var user = await _factory.CreateTestUserAsync("wrongpassuser", "wrongpass@example.com", "CorrectPass123!");

        var request = new LoginRequest
        {
            Email = "wrongpass@example.com",
            Password = "WrongPassword123!",
            RememberMe = false
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldReturn401Unauthorized()
    {
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword123!",
            RememberMe = false
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturn200AndNewTokens()
    {
        var password = "RefreshPass123!";
        var user = await _factory.CreateTestUserAsync("refreshuser", "refresh@example.com", password);

        var loginRequest = new LoginRequest
        {
            Email = "refresh@example.com",
            Password = password,
            RememberMe = false
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResult!.Data!.RefreshToken
        };

        await Task.Delay(100);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>();
        refreshResult.Should().NotBeNull();
        refreshResult!.Success.Should().BeTrue();
        refreshResult.Data.Should().NotBeNull();
        refreshResult.Data!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.Data.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResult.Data.AccessToken.Should().NotBe(loginResult.Data.AccessToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturn401Unauthorized()
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid_refresh_token_hash"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid refresh token");
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldReturn200()
    {
        var password = "LogoutPass123!";
        var user = await _factory.CreateTestUserAsync("logoutuser", "logout@example.com", password);

        var loginRequest = new LoginRequest
        {
            Email = "logout@example.com",
            Password = password,
            RememberMe = false
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult!.Data!.AccessToken);

        var logoutRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResult.Data.RefreshToken
        };

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await logoutResponse.Content.ReadFromJsonAsync<ApiResponse<string>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().Contain("Logged out successfully");
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ShouldReturn401Unauthorized()
    {
        var password = "LogoutInvalidPass123!";
        var user = await _factory.CreateTestUserAsync("logoutinvalid", "logoutinvalid@example.com", password);

        var loginRequest = new LoginRequest
        {
            Email = "logoutinvalid@example.com",
            Password = password,
            RememberMe = false
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult!.Data!.AccessToken);

        var logoutRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid_or_nonexistent_token"
        };

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var result = await logoutResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_WithAlreadyRevokedToken_ShouldReturn401Unauthorized()
    {
        var password = "LogoutRevokedPass123!";
        var user = await _factory.CreateTestUserAsync("logoutrevoked", "logoutrevoked@example.com", password);

        var loginRequest = new LoginRequest
        {
            Email = "logoutrevoked@example.com",
            Password = password,
            RememberMe = false
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult!.Data!.AccessToken);

        var logoutRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResult.Data.RefreshToken
        };

        var firstLogoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);
        firstLogoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondLogoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        secondLogoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var result = await secondLogoutResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        var request = new RefreshTokenRequest
        {
            RefreshToken = "some_token"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/logout", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_ShouldReturn200()
    {
        var oldPassword = "OldSecurePass123!";
        var user = await _factory.CreateTestUserAsync("changepassuser", "changepass@example.com", oldPassword);

        var loginRequest = new LoginRequest
        {
            Email = "changepass@example.com",
            Password = oldPassword,
            RememberMe = false
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult!.Data!.AccessToken);

        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = oldPassword,
            NewPassword = "NewSecurePass123!",
            ConfirmPassword = "NewSecurePass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().Contain("Password changed successfully");
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_ShouldReturn400BadRequest()
    {
        var correctPassword = "CorrectPass123!";
        var user = await _factory.CreateTestUserAsync("wrongcurrentpass", "wrongcurrent@example.com", correctPassword);

        var loginRequest = new LoginRequest
        {
            Email = "wrongcurrent@example.com",
            Password = correctPassword,
            RememberMe = false
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginResult!.Data!.AccessToken);

        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "WrongCurrentPass123!",
            NewPassword = "NewSecurePass123!",
            ConfirmPassword = "NewSecurePass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("incorrect");
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/change-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/auth/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
        content.Should().Contain("auth");
    }
}
