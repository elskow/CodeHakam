using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;

namespace ContentService.Services.Implementations;

public class TestCaseService(
    ITestCaseRepository testCaseRepository,
    IProblemRepository problemRepository,
    IStorageService storageService,
    IEventPublisher eventPublisher,
    IConfiguration configuration,
    ILogger<TestCaseService> logger)
    : ITestCaseService
{
    private readonly string _bucketName = configuration["MinIO:BucketName"] ?? "codehakam-testcases";
    private readonly long _maxTestCaseFileSize = long.Parse(configuration["ContentService:MaxTestCaseFileSize"] ?? "10485760");

    public async Task<TestCase?> GetTestCaseAsync(long id, CancellationToken cancellationToken = default)
    {
        return await testCaseRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<TestCase>> GetTestCasesAsync(long problemId, bool samplesOnly, CancellationToken cancellationToken = default)
    {
        if (samplesOnly)
        {
            return await testCaseRepository.GetSampleTestCasesAsync(problemId);
        }

        return await testCaseRepository.GetByProblemIdAsync(problemId);
    }

    public async Task<int> GetTestCaseCountAsync(long problemId, CancellationToken cancellationToken = default)
    {
        return await testCaseRepository.GetCountByProblemAsync(problemId);
    }

    public async Task<int> GetSampleTestCaseCountAsync(long problemId, CancellationToken cancellationToken = default)
    {
        var sampleTestCases = await testCaseRepository.GetSampleTestCasesAsync(problemId);
        return sampleTestCases.Count();
    }

    public async Task<TestCase> UploadTestCaseAsync(
        long problemId,
        IFormFile inputFile,
        IFormFile outputFile,
        bool isSample,
        int testNumber,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var problem = await problemRepository.GetByIdAsync(problemId);
        if (problem == null)
        {
            throw new KeyNotFoundException($"Problem with ID {problemId} not found");
        }

        if (problem.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the problem author can upload test cases");
        }

        // Validate file sizes
        if (!await ValidateTestCaseSizeAsync(inputFile.Length, cancellationToken))
        {
            throw new InvalidOperationException($"Input file size exceeds maximum allowed size of {_maxTestCaseFileSize} bytes");
        }

        if (!await ValidateTestCaseSizeAsync(outputFile.Length, cancellationToken))
        {
            throw new InvalidOperationException($"Output file size exceeds maximum allowed size of {_maxTestCaseFileSize} bytes");
        }

        // Generate unique object names for MinIO
        var inputObjectName = $"problem-{problemId}/test-{testNumber}/input.txt";
        var outputObjectName = $"problem-{problemId}/test-{testNumber}/output.txt";

        try
        {
            // Upload input file
            await using (var inputStream = inputFile.OpenReadStream())
            {
                await storageService.UploadFileAsync(
                    _bucketName,
                    inputObjectName,
                    inputStream,
                    inputFile.Length,
                    "text/plain",
                    cancellationToken);
            }

            // Upload output file
            await using (var outputStream = outputFile.OpenReadStream())
            {
                await storageService.UploadFileAsync(
                    _bucketName,
                    outputObjectName,
                    outputStream,
                    outputFile.Length,
                    "text/plain",
                    cancellationToken);
            }

            // Create test case record
            var testCase = new TestCase
            {
                ProblemId = problemId,
                TestNumber = testNumber,
                IsSample = isSample,
                InputFileUrl = $"{_bucketName}/{inputObjectName}",
                OutputFileUrl = $"{_bucketName}/{outputObjectName}",
                CreatedAt = DateTime.UtcNow
            };

            await testCaseRepository.CreateAsync(testCase);

            logger.LogInformation(
                "Test case uploaded: Problem {ProblemId}, Test {TestNumber}, Sample: {IsSample}",
                problemId, testNumber, isSample);

            // Publish event
            await eventPublisher.PublishTestCaseUploadedAsync(
                testCase.Id,
                problemId,
                testNumber,
                isSample,
                cancellationToken);

            return testCase;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload test case for problem {ProblemId}", problemId);

            // Clean up uploaded files if test case creation failed
            try
            {
                if (await storageService.FileExistsAsync(_bucketName, inputObjectName, cancellationToken))
                {
                    await storageService.DeleteFileAsync(_bucketName, inputObjectName, cancellationToken);
                }

                if (await storageService.FileExistsAsync(_bucketName, outputObjectName, cancellationToken))
                {
                    await storageService.DeleteFileAsync(_bucketName, outputObjectName, cancellationToken);
                }
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(cleanupEx, "Failed to clean up files after upload failure");
            }

            throw;
        }
    }

    public async Task DeleteTestCaseAsync(long testCaseId, long userId, CancellationToken cancellationToken = default)
    {
        var testCase = await testCaseRepository.GetByIdAsync(testCaseId);
        if (testCase == null)
        {
            throw new KeyNotFoundException($"Test case with ID {testCaseId} not found");
        }

        var problem = await problemRepository.GetByIdAsync(testCase.ProblemId);
        if (problem == null)
        {
            throw new KeyNotFoundException($"Problem with ID {testCase.ProblemId} not found");
        }

        if (problem.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the problem author can delete test cases");
        }

        // Delete files from storage
        try
        {
            var inputObjectName = testCase.InputFileUrl.Replace($"{_bucketName}/", "");
            var outputObjectName = testCase.OutputFileUrl.Replace($"{_bucketName}/", "");

            if (!string.IsNullOrEmpty(inputObjectName) && await storageService.FileExistsAsync(_bucketName, inputObjectName, cancellationToken))
            {
                await storageService.DeleteFileAsync(_bucketName, inputObjectName, cancellationToken);
            }

            if (!string.IsNullOrEmpty(outputObjectName) && await storageService.FileExistsAsync(_bucketName, outputObjectName, cancellationToken))
            {
                await storageService.DeleteFileAsync(_bucketName, outputObjectName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete test case files from storage for test case {TestCaseId}", testCaseId);
        }

        // Delete test case record
        await testCaseRepository.DeleteAsync(testCaseId);

        logger.LogInformation("Test case deleted: {TestCaseId}", testCaseId);

        // Publish event
        await eventPublisher.PublishTestCaseDeletedAsync(
            testCaseId,
            testCase.ProblemId,
            testCase.TestNumber,
            cancellationToken);
    }

    public async Task<(Stream stream, string contentType, string fileName)> DownloadTestCaseInputAsync(
        long testCaseId,
        CancellationToken cancellationToken = default)
    {
        var testCase = await testCaseRepository.GetByIdAsync(testCaseId);
        if (testCase == null)
        {
            throw new KeyNotFoundException($"Test case with ID {testCaseId} not found");
        }

        if (string.IsNullOrEmpty(testCase.InputFileUrl))
        {
            throw new InvalidOperationException($"Test case {testCaseId} does not have an input file");
        }

        var objectName = testCase.InputFileUrl.Replace($"{_bucketName}/", "");
        var stream = await storageService.DownloadFileAsync(_bucketName, objectName, cancellationToken);
        var fileName = $"test-{testCase.TestNumber}-input.txt";

        return (stream, "text/plain", fileName);
    }

    public async Task<(Stream stream, string contentType, string fileName)> DownloadTestCaseOutputAsync(
        long testCaseId,
        CancellationToken cancellationToken = default)
    {
        var testCase = await testCaseRepository.GetByIdAsync(testCaseId);
        if (testCase == null)
        {
            throw new KeyNotFoundException($"Test case with ID {testCaseId} not found");
        }

        if (string.IsNullOrEmpty(testCase.OutputFileUrl))
        {
            throw new InvalidOperationException($"Test case {testCaseId} does not have an output file");
        }

        var objectName = testCase.OutputFileUrl.Replace($"{_bucketName}/", "");
        var stream = await storageService.DownloadFileAsync(_bucketName, objectName, cancellationToken);
        var fileName = $"test-{testCase.TestNumber}-output.txt";

        return (stream, "text/plain", fileName);
    }

    public Task<bool> ValidateTestCaseSizeAsync(long size, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(size > 0 && size <= _maxTestCaseFileSize);
    }
}
