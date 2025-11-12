namespace ContentService.Services.Implementations;

using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Logging;

public class ProblemListService : IProblemListService
{
    private readonly IProblemListRepository _problemListRepository;
    private readonly IProblemRepository _problemRepository;
    private readonly ILogger<ProblemListService> _logger;

    public ProblemListService(
        IProblemListRepository problemListRepository,
        IProblemRepository problemRepository,
        ILogger<ProblemListService> logger)
    {
        _problemListRepository = problemListRepository;
        _problemRepository = problemRepository;
        _logger = logger;
    }

    public async Task<ProblemList?> GetProblemListAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _problemListRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ProblemList>> GetProblemListsByOwnerAsync(
        long ownerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _problemListRepository.GetByOwnerAsync(ownerId, page, pageSize);
    }

    public async Task<IEnumerable<ProblemList>> GetPublicListsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _problemListRepository.GetPublicListsAsync(page, pageSize);
    }

    public async Task<ProblemList> CreateProblemListAsync(
        string name,
        string description,
        bool isPublic,
        long ownerId,
        List<long> problemIds,
        CancellationToken cancellationToken = default)
    {
        await ValidateProblemsExistAsync(problemIds);

        var problemList = new ProblemList
        {
            Title = name,
            Description = description,
            IsPublic = isPublic,
            OwnerId = ownerId,
            ProblemIds = problemIds.ToArray(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _problemListRepository.CreateAsync(problemList);

        _logger.LogInformation("Problem list created: {ListId} - {Title} by user {OwnerId}",
            problemList.Id, problemList.Title, ownerId);

        return problemList;
    }

    public async Task<ProblemList> UpdateProblemListAsync(
        long id,
        string name,
        string description,
        bool isPublic,
        List<long> problemIds,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var problemList = await _problemListRepository.GetByIdAsync(id);
        if (problemList == null)
        {
            throw new InvalidOperationException($"Problem list with ID {id} not found");
        }

        if (problemList.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only the list owner can update it");
        }

        await ValidateProblemsExistAsync(problemIds);

        problemList.Title = name;
        problemList.Description = description;
        problemList.IsPublic = isPublic;
        problemList.ProblemIds = problemIds.ToArray();
        problemList.UpdatedAt = DateTime.UtcNow;

        await _problemListRepository.UpdateAsync(problemList);

        _logger.LogInformation("Problem list updated: {ListId} - {Title} by user {UserId}",
            problemList.Id, problemList.Title, userId);

        return problemList;
    }

    public async Task DeleteProblemListAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var problemList = await _problemListRepository.GetByIdAsync(id);
        if (problemList == null)
        {
            throw new InvalidOperationException($"Problem list with ID {id} not found");
        }

        if (problemList.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only the list owner can delete it");
        }

        await _problemListRepository.DeleteAsync(id);

        _logger.LogInformation("Problem list deleted: {ListId} by user {UserId}", id, userId);
    }

    public async Task AddProblemToListAsync(long listId, long problemId, long userId, CancellationToken cancellationToken = default)
    {
        var problemList = await _problemListRepository.GetByIdAsync(listId);
        if (problemList == null)
        {
            throw new InvalidOperationException($"Problem list with ID {listId} not found");
        }

        if (problemList.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only the list owner can modify it");
        }

        var problemExists = await _problemRepository.ExistsAsync(problemId);
        if (!problemExists)
        {
            throw new InvalidOperationException($"Problem with ID {problemId} not found");
        }

        if (problemList.ProblemIds.Contains(problemId))
        {
            throw new InvalidOperationException($"Problem {problemId} is already in the list");
        }

        var newProblemIds = new List<long>(problemList.ProblemIds) { problemId };
        problemList.ProblemIds = newProblemIds.ToArray();
        problemList.UpdatedAt = DateTime.UtcNow;

        await _problemListRepository.UpdateAsync(problemList);

        _logger.LogInformation("Problem {ProblemId} added to list {ListId} by user {UserId}",
            problemId, listId, userId);
    }

    public async Task RemoveProblemFromListAsync(long listId, long problemId, long userId, CancellationToken cancellationToken = default)
    {
        var problemList = await _problemListRepository.GetByIdAsync(listId);
        if (problemList == null)
        {
            throw new InvalidOperationException($"Problem list with ID {listId} not found");
        }

        if (problemList.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only the list owner can modify it");
        }

        if (!problemList.ProblemIds.Contains(problemId))
        {
            throw new InvalidOperationException($"Problem {problemId} is not in the list");
        }

        var newProblemIds = problemList.ProblemIds.Where(id => id != problemId).ToArray();
        problemList.ProblemIds = newProblemIds;
        problemList.UpdatedAt = DateTime.UtcNow;

        await _problemListRepository.UpdateAsync(problemList);

        _logger.LogInformation("Problem {ProblemId} removed from list {ListId} by user {UserId}",
            problemId, listId, userId);
    }

    public async Task<bool> ProblemListExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        var problemList = await _problemListRepository.GetByIdAsync(id);
        return problemList != null;
    }

    public async Task<bool> IsOwnerAsync(long listId, long userId, CancellationToken cancellationToken = default)
    {
        var problemList = await _problemListRepository.GetByIdAsync(listId);
        return problemList?.OwnerId == userId;
    }

    private async Task ValidateProblemsExistAsync(List<long> problemIds)
    {
        foreach (var problemId in problemIds)
        {
            var exists = await _problemRepository.ExistsAsync(problemId);
            if (!exists)
            {
                throw new InvalidOperationException($"Problem with ID {problemId} not found");
            }
        }
    }
}
