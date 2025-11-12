namespace ContentService.Services.Interfaces;

using ContentService.Models;

public interface IEditorialService
{
    Task<Editorial?> GetEditorialAsync(long problemId, CancellationToken cancellationToken = default);

    Task<Editorial?> GetEditorialByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<Editorial> CreateEditorialAsync(
        long problemId,
        string content,
        string approach,
        string complexity,
        long authorId,
        CancellationToken cancellationToken = default);

    Task<Editorial> UpdateEditorialAsync(
        long problemId,
        string content,
        string approach,
        string complexity,
        long userId,
        CancellationToken cancellationToken = default);

    Task PublishEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default);

    Task UnpublishEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default);

    Task DeleteEditorialAsync(long problemId, long userId, CancellationToken cancellationToken = default);

    Task<bool> EditorialExistsAsync(long problemId, CancellationToken cancellationToken = default);

    Task<bool> IsAuthorOrAdminAsync(long problemId, long userId, bool isAdmin, CancellationToken cancellationToken = default);
}
