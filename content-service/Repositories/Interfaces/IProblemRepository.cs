using ContentService.Enums;
using ContentService.Models;

namespace ContentService.Repositories.Interfaces;

public interface IProblemRepository
{
    Task<Problem?> GetByIdAsync(long id, bool includeRelated = false);
    Task<Problem?> GetBySlugAsync(string slug, bool includeRelated = false);
    Task<IEnumerable<Problem>> GetAllAsync(int page, int pageSize);
    Task<IEnumerable<Problem>> SearchAsync(
        string? searchTerm,
        Difficulty? difficulty,
        string? tag,
        ProblemVisibility? visibility,
        int page,
        int pageSize);
    Task<int> GetTotalCountAsync();
    Task<int> GetSearchCountAsync(
        string? searchTerm,
        Difficulty? difficulty,
        string? tag,
        ProblemVisibility? visibility);
    Task<Problem> CreateAsync(Problem problem);
    Task<Problem> UpdateAsync(Problem problem);
    Task<bool> DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
    Task<bool> SlugExistsAsync(string slug);
    Task<IEnumerable<Problem>> GetByAuthorAsync(long authorId, int page, int pageSize);
    Task IncrementViewCountAsync(long id);
    Task UpdateStatisticsAsync(long id, int submissionCount, int acceptedCount);
    Task<IEnumerable<string>> GetAllTagsAsync();
}
