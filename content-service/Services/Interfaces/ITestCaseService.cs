namespace ContentService.Services.Interfaces;

using ContentService.Models;

public interface ITestCaseService
{
    Task<TestCase?> GetTestCaseAsync(long id, CancellationToken cancellationToken = default);

    Task<IEnumerable<TestCase>> GetTestCasesAsync(long problemId, bool samplesOnly, CancellationToken cancellationToken = default);

    Task<int> GetTestCaseCountAsync(long problemId, CancellationToken cancellationToken = default);

    Task<TestCase> UploadTestCaseAsync(
        long problemId,
        Stream inputData,
        Stream outputData,
        long inputSize,
        long outputSize,
        bool isSample,
        int orderIndex,
        long userId,
        CancellationToken cancellationToken = default);

    Task DeleteTestCaseAsync(long testCaseId, long userId, CancellationToken cancellationToken = default);

    Task<Stream> DownloadTestCaseInputAsync(long testCaseId, CancellationToken cancellationToken = default);

    Task<Stream> DownloadTestCaseOutputAsync(long testCaseId, CancellationToken cancellationToken = default);

    Task<bool> ValidateTestCaseSizeAsync(long size, CancellationToken cancellationToken = default);
}
