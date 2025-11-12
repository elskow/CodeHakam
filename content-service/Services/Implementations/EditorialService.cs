using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;

namespace ContentService.Services.Implementations;

public class EditorialService(
    IEditorialRepository editorialRepository,
    IProblemRepository problemRepository,
    IEventPublisher eventPublisher,
    ILogger<EditorialService> logger)
    : IEditorialService
{
    public async Task<Editorial?> GetEditorialAsync(long problemId, CancellationToken cancellationToken = default)
    {
        return await editorialRepository.GetByProblemIdAsync(problemId);
    }

    public async Task<Editorial?> GetEditorialByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await editorialRepository.GetByIdAsync(id);
    }

    public async Task<Editorial> CreateEditorialAsync(
        long problemId,
        string content,
        string approach,
        string complexity,
        long authorId,
        CancellationToken cancellationToken = default)
    {
        var problem = await problemRepository.GetByIdAsync(problemId);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {problemId} not found");
        }

        if (problem.AuthorId != authorId)
        {
            throw new UnauthorizedAccessException("Only the problem author can create an editorial");
        }

        var existingEditorial = await editorialRepository.GetByProblemIdAsync(problemId);
        if (existingEditorial != null)
        {
            throw new InvalidOperationException($"Editorial already exists for problem {problemId}");
        }

        var editorial = new Editorial
        {
            ProblemId = problemId,
            Content = content,
            Approach = approach,
            TimeComplexity = complexity,
            SpaceComplexity = complexity,
            AuthorId = authorId,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await editorialRepository.CreateAsync(editorial);

        logger.LogInformation("Editorial created for problem {ProblemId} by user {AuthorId}",
            problemId, authorId);

        return editorial;
    }

    public async Task<Editorial> UpdateEditorialAsync(
        long problemId,
        string content,
        string approach,
        string complexity,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var editorial = await editorialRepository.GetByProblemIdAsync(problemId);
        if (editorial == null)
        {
            throw new InvalidOperationException($"Editorial not found for problem {problemId}");
        }

        if (editorial.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the editorial author can update it");
        }

        editorial.Content = content;
        editorial.Approach = approach;
        editorial.TimeComplexity = complexity;
        editorial.SpaceComplexity = complexity;
        editorial.UpdatedAt = DateTime.UtcNow;

        await editorialRepository.UpdateAsync(editorial);

        logger.LogInformation("Editorial updated for problem {ProblemId} by user {UserId}",
            problemId, userId);

        return editorial;
    }

    public async Task<Editorial> CreateOrUpdateEditorialAsync(
        long problemId,
        long authorId,
        string content,
        string timeComplexity,
        string spaceComplexity,
        string? videoUrl = null,
        CancellationToken cancellationToken = default)
    {
        var problem = await problemRepository.GetByIdAsync(problemId);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {problemId} not found");
        }

        if (problem.AuthorId != authorId)
        {
            throw new UnauthorizedAccessException("Only the problem author can create/update an editorial");
        }

        var existingEditorial = await editorialRepository.GetByProblemIdAsync(problemId);

        if (existingEditorial != null)
        {
            // Update existing editorial
            existingEditorial.Content = content;
            existingEditorial.TimeComplexity = timeComplexity;
            existingEditorial.SpaceComplexity = spaceComplexity;
            existingEditorial.VideoUrl = videoUrl;
            existingEditorial.UpdatedAt = DateTime.UtcNow;

            await editorialRepository.UpdateAsync(existingEditorial);

            logger.LogInformation("Editorial updated for problem {ProblemId} by user {AuthorId}",
                problemId, authorId);

            return existingEditorial;
        }
        // Create new editorial
        var editorial = new Editorial
        {
            ProblemId = problemId,
            Content = content,
            Approach = string.Empty,
            TimeComplexity = timeComplexity,
            SpaceComplexity = spaceComplexity,
            VideoUrl = videoUrl,
            AuthorId = authorId,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await editorialRepository.CreateAsync(editorial);

        logger.LogInformation("Editorial created for problem {ProblemId} by user {AuthorId}",
            problemId, authorId);

        return editorial;
    }

    public async Task PublishEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default)
    {
        var editorial = await editorialRepository.GetByProblemIdAsync(problemId);
        if (editorial == null)
        {
            throw new InvalidOperationException($"Editorial not found for problem {problemId}");
        }

        if (editorial.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the editorial author can publish it");
        }

        if (editorial.IsPublished)
        {
            throw new InvalidOperationException($"Editorial for problem {problemId} is already published");
        }

        await editorialRepository.PublishAsync(editorial.Id);

        logger.LogInformation("Editorial published for problem {ProblemId} by user {UserId}",
            problemId, userId);

        await eventPublisher.PublishEditorialPublishedAsync(
            problemId,
            editorial.Id,
            userId,
            cancellationToken);
    }

    public async Task UnpublishEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default)
    {
        var editorial = await editorialRepository.GetByProblemIdAsync(problemId);
        if (editorial == null)
        {
            throw new InvalidOperationException($"Editorial not found for problem {problemId}");
        }

        if (editorial.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the editorial author can unpublish it");
        }

        if (!editorial.IsPublished)
        {
            throw new InvalidOperationException($"Editorial for problem {problemId} is not published");
        }

        editorial.IsPublished = false;
        editorial.PublishedAt = null;
        editorial.UpdatedAt = DateTime.UtcNow;

        await editorialRepository.UpdateAsync(editorial);

        logger.LogInformation("Editorial unpublished for problem {ProblemId} by user {UserId}",
            problemId, userId);
    }

    public async Task DeleteEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default)
    {
        var editorial = await editorialRepository.GetByProblemIdAsync(problemId);
        if (editorial == null)
        {
            throw new InvalidOperationException($"Editorial not found for problem {problemId}");
        }

        if (editorial.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the editorial author can delete it");
        }

        await editorialRepository.DeleteAsync(editorial.Id);

        logger.LogInformation("Editorial deleted for problem {ProblemId} by user {UserId}",
            problemId, userId);
    }

    public async Task<bool> EditorialExistsAsync(long problemId, CancellationToken cancellationToken = default)
    {
        var editorial = await editorialRepository.GetByProblemIdAsync(problemId);
        return editorial != null;
    }

    public async Task<bool> IsAuthorOrAdminAsync(long problemId, long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return true;
        }

        var editorial = await editorialRepository.GetByProblemIdAsync(problemId);
        return editorial?.AuthorId == userId;
    }
}
