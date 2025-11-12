namespace ContentService.Services.Implementations;

using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class TestCaseService : ITestCaseService
{
    private readonly ITestCaseRepository _testCaseRepository;
    private readonly IProblemRepository _problemRepository;
    private readonly IStorageService _storageService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TestCaseService> _logger;
    private readonly long _maxTestCaseFileSize;
    private readonly string _bucketName;

    public TestCaseService(
        ITestCaseRepository testCaseRepository,
        IProblemRepository problemRepository,
        IStorageService storageService,
        IEventPublisher eventPublisher,
        IConfiguration configuration,
        ILogger<TestCaseService> logger)
    {
        _testCaseRepository = testCaseRepository;
        _problemRepository = problemRepository;
        _storageService = storageService;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _maxTestCaseFileSize = long.Parse(configuration["ContentService:MaxTestCaseFileSize"] ?? "10485760");
        _bucketName = configuration["MinIO:BucketName"] ?? "codehakam-testcases";
    }

    public async Task<TestCase?> GetTestCaseAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _testCaseRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<TestCase>> GetTestCasesAsync(long problemId, bool samplesOnly, CancellationToken cancellationToken = default)
    {
        if (samplesOnly)
        {
            return await _testCaseRepository.GetSampleTestCasesAsync(problemId);
        }

        return await _testCaseRepository.GetByProblemIdAsync(problemId);
    }

    public async Task<int> GetTestCaseCountAsync(long problemId, CancellationToken cancellationToken = default)
    {
        return await _testCaseRepository.GetCountByProblemAsync(problemId);
    }

    public async Task<TestCase> UploadTestCaseAsync(
        long problemId,
        Stream inputData,
        Stream outputData,
        long inputSize,
        long outputSize,
        bool isSample,
        int orderIndex,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var problem = await _problemRepository.GetByIdAsync(problemId);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {problemId} not found");
        }

        if (problem.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the problem author can upload test cases");
        }

        if (!await ValidateTestCaseSizeAsync(inputSize, cancellationToken) ||
            !await ValidateTestCaseSizeAsync(outputSize, cancellationToken))
        {
            throw new InvalidOperationException($"Test case file size exceeds maximum allowed size of {_maxTestCaseFileSize} bytes");
        }

        var testCaseId = Guid.NewGuid().ToString();
        var inputPath = $"problems/{problemId}/testcases/{testCaseId}/input.txt";
        var outputPath = $"problems/{problemId}/testcases/{testCaseId}/output.txt";

        await _storageService.EnsureBucketExistsAsync(_bucketName, cancellationToken);

        await _storageService.UploadFileAsync(
            _bucketName,
            inputPath,
            inputData,
            inputSize,
            "text/plain",
            cancellationToken);

        await _storageService.UploadFileAsync(
            _bucketName,
            outputPath,
            outputData,
            outputSize,
            "text/plain",
            cancellationToken);

        var testCase = new TestCase
        {
            ProblemId = problemId,
            InputFileUrl = inputPath,
            OutputFileUrl = outputPath,
            IsSample = isSample,
            TestNumber = orderIndex,
            InputSize = inputSize,
            OutputSize = outputSize,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _testCaseRepository.CreateAsync(testCase);

        _logger.LogInformation("Test case uploaded for problem {ProblemId}: {TestCaseId}, IsSample: {IsSample}",
            problemId, testCase.Id, isSample);

        await _eventPublisher.PublishTestCaseUploadedAsync(
            problemId,
            testCase.Id,
            isSample,
            cancellationToken);

        return testCase;
    }

    public async Task DeleteTestCaseAsync(long testCaseId, long userId, CancellationToken cancellationToken = default)
    {
        var testCase = await _testCaseRepository.GetByIdAsync(testCaseId);
        if (testCase == null)
        {
            throw new InvalidOperationException($"Test case with ID {testCaseId} not found");
        }

        var problem = await _problemRepository.GetByIdAsync(testCase.ProblemId);
        if (problem == null)
        {
            throw new InvalidOperationException($"Problem with ID {testCase.ProblemId} not found");
        }

        if (problem.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the problem author can delete test cases");
        }

        await _storageService.DeleteFileAsync(_bucketName, testCase.InputFileUrl, cancellationToken);
        await _storageService.DeleteFileAsync(_bucketName, testCase.OutputFileUrl, cancellationToken);

        await _testCaseRepository.DeleteAsync(testCaseId);

        _logger.LogInformation("Test case deleted: {TestCaseId} for problem {ProblemId}",
            testCaseId, testCase.ProblemId);
    }

    public async Task<Stream> DownloadTestCaseInputAsync(long testCaseId, CancellationToken cancellationToken = default)
    {
        var testCase = await _testCaseRepository.GetByIdAsync(testCaseId);
        if (testCase == null)
        {
            throw new InvalidOperationException($"Test case with ID {testCaseId} not found");
        }

        return await _storageService.DownloadFileAsync(_bucketName, testCase.InputFileUrl, cancellationToken);
    }

    public async Task<Stream> DownloadTestCaseOutputAsync(long testCaseId, CancellationToken cancellationToken = default)
    {
        var testCase = await _testCaseRepository.GetByIdAsync(testCaseId);
        if (testCase == null)
        {
            throw new InvalidOperationException($"Test case with ID {testCaseId} not found");
        }

        return await _storageService.DownloadFileAsync(_bucketName, testCase.OutputFileUrl, cancellationToken);
    }

    public Task<bool> ValidateTestCaseSizeAsync(long size, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(size <= _maxTestCaseFileSize && size > 0);
    }
}
