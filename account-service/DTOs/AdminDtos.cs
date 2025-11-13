using System.ComponentModel.DataAnnotations;

namespace AccountService.DTOs;

public record SystemStatisticsDto
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int BannedUsers { get; init; }
    public int VerifiedUsers { get; init; }
    public int TotalProblems { get; init; }
    public int TotalSubmissions { get; init; }
    public int TotalContests { get; init; }
    public DateTime? LastUserRegistration { get; init; }
}

public record BanUserRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}

public record AssignRoleRequest
{
    [Required]
    public long RoleId { get; init; }
}
