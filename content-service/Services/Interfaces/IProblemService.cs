using ContentService.Enums;
using ContentService.Models;

namespace ContentService.Services.Interfaces;

public interface IProblemService
{
    Task<Problem?> GetProblemAsync(long id, CancellationToken cancellationToken = default);

    Task<Problem?> GetProblemBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IEnumerable<Problem>> GetProblemsAsync(
        int page,
        int pageSize,
        Difficulty? difficulty = null,
        ProblemVisibility? visibility = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Problem>> SearchProblemsAsync(
        string? searchTerm,
        Difficulty? difficulty,
        List<string>? tags,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetSearchCountAsync(
        string? searchTerm,
        Difficulty? difficulty,
        List<string>? tags,
        CancellationToken cancellationToken = default);

    Task<Problem> CreateProblemAsync(
        string title,
        string description,
        string inputFormat,
        string outputFormat,
        string constraints,
        Difficulty difficulty,
        int timeLimit,
        int memoryLimit,
        long authorId,
        List<string> tags,
        ProblemVisibility visibility = ProblemVisibility.Public,
        string? hintText = null,
        CancellationToken cancellationToken = default);

    Task<Problem> UpdateProblemAsync(
        long problemId,
        long userId,
        string? title = null,
        string? description = null,
        string? inputFormat = null,
        string? outputFormat = null,
        string? constraints = null,
        Difficulty? difficulty = null,
        int? timeLimit = null,
        int? memoryLimit = null,
        List<string>? tags = null,
        ProblemVisibility? visibility = null,
        string? hintText = null,
        CancellationToken cancellationToken = default);

    Task DeleteProblemAsync(long id, long userId, CancellationToken cancellationToken = default);

    Task IncrementViewCountAsync(long id, CancellationToken cancellationToken = default);

    Task UpdateStatisticsAsync(long id, int submissionCount, int acceptedCount, CancellationToken cancellationToken = default);

    Task<IEnumerable<Problem>> GetProblemsByAuthorAsync(long authorId, CancellationToken cancellationToken = default);

    Task<int> GetTotalProblemsCountAsync(CancellationToken cancellationToken = default);

    Task<bool> ProblemExistsAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> IsAuthorOrAdminAsync(long problemId, long userId, bool isAdmin, CancellationToken cancellationToken = default);
}
