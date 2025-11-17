using System.Text.RegularExpressions;
using ContentService.Data;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;

namespace ContentService.Services.Implementations;

public sealed class ProblemService(
    IProblemRepository problemRepository,
    ContentDbContext dbContext,
    IEventPublisher eventPublisher,
    ILogger<ProblemService> logger)
    : IProblemService
{
    public async Task<Problem?> GetProblemAsync(long id, CancellationToken cancellationToken = default)
    {
        return await problemRepository.GetByIdAsync(id, includeRelated: true);
    }

    public async Task<Problem?> GetProblemBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await problemRepository.GetBySlugAsync(slug, includeRelated: true);
    }

    public async Task<IEnumerable<Problem>> GetProblemsAsync(
        int page,
        int pageSize,
        Difficulty? difficulty = null,
        ProblemVisibility? visibility = null,
        CancellationToken cancellationToken = default)
    {
        return await problemRepository.SearchAsync(
            searchTerm: null,
            difficulty,
            tag: null,
            visibility,
            page,
            pageSize);
    }

    public async Task<IEnumerable<Problem>> SearchProblemsAsync(
        string? searchTerm,
        Difficulty? difficulty,
        List<string>? tags,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var tag = tags?.FirstOrDefault();
        return await problemRepository.SearchAsync(
            searchTerm,
            difficulty,
            tag,
            visibility: null,
            page,
            pageSize);
    }

    public async Task<int> GetSearchCountAsync(
        string? searchTerm,
        Difficulty? difficulty,
        List<string>? tags,
        CancellationToken cancellationToken = default)
    {
        var tag = tags?.FirstOrDefault();
        return await problemRepository.GetSearchCountAsync(
            searchTerm,
            difficulty,
            tag,
            visibility: null);
    }

    public async Task<Problem> CreateProblemAsync(
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
        CancellationToken cancellationToken = default)
    {
        var slug = await GenerateUniqueSlugAsync(title);

        var problem = new Problem
        {
            Title = title,
            Slug = slug,
            Description = description,
            InputFormat = inputFormat,
            OutputFormat = outputFormat,
            Constraints = constraints,
            Difficulty = difficulty,
            TimeLimit = timeLimit,
            MemoryLimit = memoryLimit,
            AuthorId = authorId,
            Visibility = visibility,
            HintText = hintText,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Normalize tags
        var problemTags = tags.Select(t => new ProblemTag
        {
            Tag = t.Trim().ToLowerInvariant()
        }).ToList();

        problem.Tags = problemTags;

        await problemRepository.CreateAsync(problem);

        logger.LogInformation("Problem created: {ProblemId} - {Title} by user {AuthorId}",
            problem.Id, problem.Title, authorId);

        await eventPublisher.PublishProblemCreatedAsync(
            problem.Id,
            problem.Title,
            problem.Slug,
            authorId,
            cancellationToken);

        return problem;
    }

    public async Task<Problem> UpdateProblemAsync(
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
        CancellationToken cancellationToken = default)
    {
        var problem = await problemRepository.GetByIdAsync(problemId, includeRelated: true);
        if (problem == null)
        {
            throw new KeyNotFoundException($"Problem with ID {problemId} not found");
        }

        if (!await IsAuthorOrAdminAsync(problemId, userId, false, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User {userId} is not authorized to update problem {problemId}");
        }

        // Update only non-null fields
        if (title != null)
        {
            problem.Title = title;
        }
        if (description != null)
        {
            problem.Description = description;
        }
        if (inputFormat != null)
        {
            problem.InputFormat = inputFormat;
        }
        if (outputFormat != null)
        {
            problem.OutputFormat = outputFormat;
        }
        if (constraints != null)
        {
            problem.Constraints = constraints;
        }
        if (difficulty.HasValue)
        {
            problem.Difficulty = difficulty.Value;
        }
        if (timeLimit.HasValue)
        {
            problem.TimeLimit = timeLimit.Value;
        }
        if (memoryLimit.HasValue)
        {
            problem.MemoryLimit = memoryLimit.Value;
        }
        if (visibility.HasValue)
        {
            problem.Visibility = visibility.Value;
        }
        if (hintText != null)
        {
            problem.HintText = hintText;
        }

        if (tags != null)
        {
            problem.Tags.Clear();
            foreach (var tag in tags)
            {
                problem.Tags.Add(new ProblemTag
                {
                    Tag = tag.Trim().ToLowerInvariant(),
                    ProblemId = problemId
                });
            }
        }

        problem.UpdatedAt = DateTime.UtcNow;

        await problemRepository.UpdateAsync(problem);

        logger.LogInformation("Problem updated: {ProblemId} - {Title} by user {UserId}",
            problem.Id, problem.Title, userId);

        await eventPublisher.PublishProblemUpdatedAsync(
            problem.Id,
            problem.Title,
            userId,
            cancellationToken);

        return problem;
    }

    public async Task DeleteProblemAsync(long id, long userId, CancellationToken cancellationToken = default)
    {
        var problem = await problemRepository.GetByIdAsync(id);
        if (problem == null)
        {
            throw new KeyNotFoundException($"Problem with ID {id} not found");
        }

        if (!await IsAuthorOrAdminAsync(id, userId, false, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User {userId} is not authorized to delete problem {id}");
        }

        await problemRepository.DeleteAsync(id);

        logger.LogInformation("Problem deleted: {ProblemId} by user {UserId}", id, userId);

        await eventPublisher.PublishProblemDeletedAsync(
            problem.Id,
            problem.Title,
            userId,
            cancellationToken);
    }

    public async Task IncrementViewCountAsync(long id, CancellationToken cancellationToken = default)
    {
        await problemRepository.IncrementViewCountAsync(id);
    }

    public async Task UpdateStatisticsAsync(
        long id,
        int submissionCount,
        int acceptedCount,
        CancellationToken cancellationToken = default)
    {
        await problemRepository.UpdateStatisticsAsync(id, submissionCount, acceptedCount);

        logger.LogInformation("Problem statistics updated: {ProblemId} - Submissions: {SubmissionCount}, Accepted: {AcceptedCount}",
            id, submissionCount, acceptedCount);
    }

    public async Task<IEnumerable<Problem>> GetProblemsByAuthorAsync(long authorId, CancellationToken cancellationToken = default)
    {
        return await problemRepository.GetByAuthorAsync(authorId, page: 1, int.MaxValue);
    }

    public async Task<int> GetTotalProblemsCountAsync(CancellationToken cancellationToken = default)
    {
        return await problemRepository.GetTotalCountAsync();
    }

    public async Task<bool> ProblemExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await problemRepository.ExistsAsync(id);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await problemRepository.SlugExistsAsync(slug);
    }

    public async Task<bool> IsAuthorOrAdminAsync(long problemId, long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return true;
        }

        var problem = await problemRepository.GetByIdAsync(problemId);
        return problem?.AuthorId == userId;
    }
    private async Task<UserProfile?> GetUserProfileAsync(long userId)
    {
        return await dbContext.UserProfiles.FindAsync(userId);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title)
    {
        var baseSlug = GenerateSlug(title);
        var slug = baseSlug;
        var counter = 1;

        while (await problemRepository.SlugExistsAsync(slug))
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
