namespace ContentService.Services.Interfaces;

using ContentService.Models;

public interface IProblemListService
{
    Task<ProblemList?> GetProblemListAsync(long id, CancellationToken cancellationToken = default);

    Task<ProblemList?> GetListAsync(long id, CancellationToken cancellationToken = default);

    Task<IEnumerable<ProblemList>> GetProblemListsByOwnerAsync(
        long ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ProblemList>> GetListsByOwnerAsync(long ownerId, CancellationToken cancellationToken = default);

    Task<IEnumerable<ProblemList>> GetPublicListsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetPublicListsCountAsync(CancellationToken cancellationToken = default);

    Task<ProblemList> CreateProblemListAsync(
        string name,
        string description,
        bool isPublic,
        long ownerId,
        List<long> problemIds,
        CancellationToken cancellationToken = default);

    Task<ProblemList> CreateListAsync(
        string name,
        string? description,
        long ownerId,
        bool isPublic,
        List<long> problemIds,
        CancellationToken cancellationToken = default);

    Task<ProblemList> UpdateProblemListAsync(
        long id,
        string name,
        string description,
        bool isPublic,
        List<long> problemIds,
        long userId,
        CancellationToken cancellationToken = default);

    Task<ProblemList> UpdateListAsync(
        long listId,
        long userId,
        string name,
        string? description,
        bool isPublic,
        CancellationToken cancellationToken = default);

    Task DeleteProblemListAsync(long id, long userId, CancellationToken cancellationToken = default);

    Task DeleteListAsync(long id, long userId, CancellationToken cancellationToken = default);

    Task AddProblemToListAsync(long listId, long problemId, long userId, CancellationToken cancellationToken = default);

    Task RemoveProblemFromListAsync(long listId, long problemId, long userId, CancellationToken cancellationToken = default);

    Task<bool> ProblemListExistsAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> IsOwnerAsync(long listId, long userId, CancellationToken cancellationToken = default);
}
