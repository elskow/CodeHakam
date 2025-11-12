namespace ContentService.Services.Interfaces;

using ContentService.Models;

public interface IProblemService
{
    Task<Problem?> GetProblemAsync(long id, CancellationToken cancellationToken = default);

    Task<Problem?> GetProblemBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<(IEnumerable<Problem> Problems, int TotalCount)> GetProblemsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<Problem> Problems, int TotalCount)> SearchProblemsAsync(
        string? searchTerm,
        string? difficulty,
        List<string>? tags,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Problem> CreateProblemAsync(
        string title,
        string description,
        string difficulty,
        int timeLimit,
        int memoryLimit,
        List<string> tags,
        long authorId,
        CancellationToken cancellationToken = default);

    Task<Problem> UpdateProblemAsync(
        long id,
        string title,
        string description,
        string difficulty,
        int timeLimit,
        int memoryLimit,
        List<string> tags,
        long userId,
        CancellationToken cancellationToken = default);

    Task DeleteProblemAsync(long id, long userId, CancellationToken cancellationToken = default);

    Task IncrementViewCountAsync(long id, CancellationToken cancellationToken = default);

    Task UpdateStatisticsAsync(long id, int submissionCount, int acceptedCount, CancellationToken cancellationToken = default);

    Task<IEnumerable<Problem>> GetProblemsByAuthorAsync(long authorId, CancellationToken cancellationToken = default);

    Task<bool> ProblemExistsAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> IsAuthorOrAdminAsync(long problemId, long userId, bool isAdmin, CancellationToken cancellationToken = default);
}
