namespace ContentService.Services.Implementations;

using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Logging;

public class EditorialService : IEditorialService
{
    private readonly IEditorialRepository _editorialRepository;
    private readonly IProblemRepository _problemRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<EditorialService> _logger;

    public EditorialService(
        IEditorialRepository editorialRepository,
        IProblemRepository problemRepository,
        IEventPublisher eventPublisher,
        ILogger<EditorialService> logger)
    {
        _editorialRepository = editorialRepository;
        _problemRepository = problemRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Editorial?> GetEditorialAsync(long problemId, CancellationToken cancellationToken = default)
    {
        return await _editorialRepository.GetByProblemIdAsync(problemId);
    }

    public async Task<Editorial?> GetEditorialByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _editorialRepository.GetByIdAsync(id);
    }

    public async Task<Editorial> CreateEditorialAsync(
        long problemId,
        string content,
        string approach,
        string complexity,
        long authorId,
        CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdAsync(problemId);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {problemId} not found");
        }

        if (problem.AuthorId != authorId)
        {
            throw new UnauthorizedAccessException("Only the problem author can create an editorial");
        }

        var existingEditorial = await _editorialRepository.GetByProblemIdAsync(problemId);
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

        await _editorialRepository.CreateAsync(editorial);

        _logger.LogInformation("Editorial created for problem {ProblemId} by user {AuthorId}",
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
        var editorial = await _editorialRepository.GetByProblemIdAsync(problemId);
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

        await _editorialRepository.UpdateAsync(editorial);

        _logger.LogInformation("Editorial updated for problem {ProblemId} by user {UserId}",
            problemId, userId);

        return editorial;
    }

    public async Task PublishEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default)
    {
        var editorial = await _editorialRepository.GetByProblemIdAsync(problemId);
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

        await _editorialRepository.PublishAsync(editorial.Id);

        _logger.LogInformation("Editorial published for problem {ProblemId} by user {UserId}",
            problemId, userId);

        await _eventPublisher.PublishEditorialPublishedAsync(
            problemId,
            editorial.Id,
            userId,
            cancellationToken);
    }

    public async Task UnpublishEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default)
    {
        var editorial = await _editorialRepository.GetByProblemIdAsync(problemId);
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

        await _editorialRepository.UpdateAsync(editorial);

        _logger.LogInformation("Editorial unpublished for problem {ProblemId} by user {UserId}",
            problemId, userId);
    }

    public async Task DeleteEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default)
    {
        var editorial = await _editorialRepository.GetByProblemIdAsync(problemId);
        if (editorial == null)
        {
            throw new InvalidOperationException($"Editorial not found for problem {problemId}");
        }

        if (editorial.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the editorial author can delete it");
        }

        await _editorialRepository.DeleteAsync(editorial.Id);

        _logger.LogInformation("Editorial deleted for problem {ProblemId} by user {UserId}",
            problemId, userId);
    }

    public async Task<bool> EditorialExistsAsync(long problemId, CancellationToken cancellationToken = default)
    {
        var editorial = await _editorialRepository.GetByProblemIdAsync(problemId);
        return editorial != null;
    }

    public async Task<bool> IsAuthorOrAdminAsync(long problemId, long userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return true;
        }

        var editorial = await _editorialRepository.GetByProblemIdAsync(problemId);
        return editorial?.AuthorId == userId;
    }
}
