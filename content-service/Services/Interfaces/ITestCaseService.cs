using ContentService.Models;

namespace ContentService.Services.Interfaces;

public interface ITestCaseService
{
    Task<TestCase?> GetTestCaseAsync(long id, CancellationToken cancellationToken = default);

    Task<IEnumerable<TestCase>> GetTestCasesAsync(long problemId, bool samplesOnly, CancellationToken cancellationToken = default);

    Task<int> GetTestCaseCountAsync(long problemId, CancellationToken cancellationToken = default);

    Task<int> GetSampleTestCaseCountAsync(long problemId, CancellationToken cancellationToken = default);

    Task<TestCase> UploadTestCaseAsync(
        long problemId,
        IFormFile inputFile,
        IFormFile outputFile,
        bool isSample,
        int testNumber,
        long userId,
        CancellationToken cancellationToken = default);

    Task DeleteTestCaseAsync(long testCaseId, long userId, CancellationToken cancellationToken = default);

    Task<(Stream stream, string contentType, string fileName)> DownloadTestCaseInputAsync(long testCaseId, CancellationToken cancellationToken = default);

    Task<(Stream stream, string contentType, string fileName)> DownloadTestCaseOutputAsync(long testCaseId, CancellationToken cancellationToken = default);

    Task<bool> ValidateTestCaseSizeAsync(long size, CancellationToken cancellationToken = default);
}
