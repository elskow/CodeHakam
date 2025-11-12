namespace ContentService.Services.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default) where T : class;

    Task PublishProblemCreatedAsync(long problemId, string title, string slug, long authorId, CancellationToken cancellationToken = default);

    Task PublishProblemUpdatedAsync(long problemId, string title, long updatedBy, CancellationToken cancellationToken = default);

    Task PublishProblemDeletedAsync(long problemId, string title, long deletedBy, CancellationToken cancellationToken = default);

    Task PublishTestCaseUploadedAsync(long testCaseId, long problemId, int testNumber, bool isSample, CancellationToken cancellationToken = default);

    Task PublishTestCaseDeletedAsync(long testCaseId, long problemId, int testNumber, CancellationToken cancellationToken = default);

    Task PublishEditorialPublishedAsync(long problemId, long editorialId, long authorId, CancellationToken cancellationToken = default);

    Task PublishDiscussionCreatedAsync(long discussionId, long problemId, string title, long authorId, CancellationToken cancellationToken = default);
}
