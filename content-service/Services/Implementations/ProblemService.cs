namespace ContentService.Services.Implementations;

using System.Text.RegularExpressions;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Logging;

public class ProblemService : IProblemService
{
    private readonly IProblemRepository _problemRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ProblemService> _logger;

    public ProblemService(
        IProblemRepository problemRepository,
        IEventPublisher eventPublisher,
        ILogger<ProblemService> logger)
    {
        _problemRepository = problemRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Problem?> GetProblemAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _problemRepository.GetByIdAsync(id, includeRelated: true);
    }

    public async Task<Problem?> GetProblemBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _problemRepository.GetBySlugAsync(slug, includeRelated: true);
    }

    public async Task<(IEnumerable<Problem> Problems, int TotalCount)> GetProblemsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var problems = await _problemRepository.GetAllAsync(page, pageSize);
        var totalCount = await _problemRepository.GetTotalCountAsync();

        return (problems, totalCount);
    }

    public async Task<(IEnumerable<Problem> Problems, int TotalCount)> SearchProblemsAsync(
        string? searchTerm,
        string? difficulty,
        List<string>? tags,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        Difficulty? difficultyEnum = null;
        if (!string.IsNullOrEmpty(difficulty))
        {
            difficultyEnum = Enum.Parse<Difficulty>(difficulty, ignoreCase: true);
        }

        var tag = tags?.FirstOrDefault();
        var problems = await _problemRepository.SearchAsync(
            searchTerm,
            difficultyEnum,
            tag,
            null,
            page,
            pageSize);

        var totalCount = await _problemRepository.GetSearchCountAsync(
            searchTerm,
            difficultyEnum,
            tag,
            null);

        return (problems, totalCount);
    }

    public async Task<Problem> CreateProblemAsync(
        string title,
        string description,
        string difficulty,
        int timeLimit,
        int memoryLimit,
        List<string> tags,
        long authorId,
        CancellationToken cancellationToken = default)
    {
        var slug = await GenerateUniqueSlugAsync(title);
        var difficultyEnum = Enum.Parse<Difficulty>(difficulty, ignoreCase: true);

        var problem = new Problem
        {
            Title = title,
            Slug = slug,
            Description = description,
            InputFormat = string.Empty,
            OutputFormat = string.Empty,
            Constraints = string.Empty,
            Difficulty = difficultyEnum,
            TimeLimit = timeLimit,
            MemoryLimit = memoryLimit,
            AuthorId = authorId,
            Visibility = ProblemVisibility.Private,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var problemTags = tags.Select(t => new ProblemTag
        {
            Tag = t.Trim().ToLowerInvariant()
        }).ToList();

        problem.Tags = problemTags;

        await _problemRepository.CreateAsync(problem);

        _logger.LogInformation("Problem created: {ProblemId} - {Title} by user {AuthorId}",
            problem.Id, problem.Title, authorId);

        await _eventPublisher.PublishProblemCreatedAsync(
            problem.Id,
            problem.Title,
            problem.Slug,
            authorId,
            cancellationToken);

        return problem;
    }

    public async Task<Problem> UpdateProblemAsync(
        long id,
        string title,
        string description,
        string difficulty,
        int timeLimit,
        int memoryLimit,
        List<string> tags,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdAsync(id);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {id} not found");
        }

        if (problem.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the author can update this problem");
        }

        var difficultyEnum = Enum.Parse<Difficulty>(difficulty, ignoreCase: true);

        problem.Title = title;
        problem.Description = description;
        problem.Difficulty = difficultyEnum;
        problem.TimeLimit = timeLimit;
        problem.MemoryLimit = memoryLimit;
        problem.UpdatedAt = DateTime.UtcNow;

        problem.Tags.Clear();
        foreach (var tag in tags)
        {
            problem.Tags.Add(new ProblemTag { Tag = tag.Trim().ToLowerInvariant() });
        }

        await _problemRepository.UpdateAsync(problem);

        _logger.LogInformation("Problem updated: {ProblemId} - {Title} by user {UserId}",
            problem.Id, problem.Title, userId);

        await _eventPublisher.PublishProblemUpdatedAsync(
            problem.Id,
            problem.Title,
            userId,
            cancellationToken);

        return problem;
    }

    public async Task DeleteProblemAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdAsync(id);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {id} not found");
        }

        if (problem.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the author can delete this problem");
        }

        await _problemRepository.DeleteAsync(id);

        _logger.LogInformation("Problem deleted: {ProblemId} by user {UserId}", id, userId);

        await _eventPublisher.PublishProblemDeletedAsync(
            problem.Id,
            problem.Title,
            userId,
            cancellationToken);
    }

    public async Task IncrementViewCountAsync(long id, CancellationToken cancellationToken = default)
    {
        await _problemRepository.IncrementViewCountAsync(id);
    }

    public async Task UpdateStatisticsAsync(
        long id,
        int submissionCount,
        int acceptedCount,
        CancellationToken cancellationToken = default)
    {
        await _problemRepository.UpdateStatisticsAsync(id, submissionCount, acceptedCount);

        _logger.LogInformation("Problem statistics updated: {ProblemId} - Submissions: {SubmissionCount}, Accepted: {AcceptedCount}",
            id, submissionCount, acceptedCount);
    }

    public async Task<IEnumerable<Problem>> GetProblemsByAuthorAsync(long authorId, CancellationToken cancellationToken = default)
    {
        return await _problemRepository.GetByAuthorAsync(authorId, 1, int.MaxValue);
    }

    public async Task<bool> ProblemExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _problemRepository.ExistsAsync(id);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _problemRepository.SlugExistsAsync(slug);
    }

    public async Task<bool> IsAuthorOrAdminAsync(long problemId, long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return true;
        }

        var problem = await _problemRepository.GetByIdAsync(problemId);
        return problem?.AuthorId == userId;
    }

    private async Task<string> GenerateUniqueSlugAsync(string title)
    {
        var baseSlug = GenerateSlug(title);
        var slug = baseSlug;
        var counter = 1;

        while (await _problemRepository.SlugExistsAsync(slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}
