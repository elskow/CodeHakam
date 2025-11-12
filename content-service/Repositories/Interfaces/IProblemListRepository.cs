using ContentService.Models;

namespace ContentService.Repositories.Interfaces;

public interface IProblemListRepository
{
    Task<ProblemList?> GetByIdAsync(long id);
    Task<IEnumerable<ProblemList>> GetByOwnerAsync(long ownerId, int page, int pageSize);
    Task<IEnumerable<ProblemList>> GetPublicListsAsync(int page, int pageSize);
    Task<int> GetTotalPublicCountAsync();
    Task<int> GetCountByOwnerAsync(long ownerId);
    Task<ProblemList> CreateAsync(ProblemList list);
    Task<ProblemList> UpdateAsync(ProblemList list);
    Task<bool> DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
    Task IncrementViewCountAsync(long id);
    Task<bool> AddProblemAsync(long listId, long problemId);
    Task<bool> RemoveProblemAsync(long listId, long problemId);
    Task<bool> ContainsProblemAsync(long listId, long problemId);
}
