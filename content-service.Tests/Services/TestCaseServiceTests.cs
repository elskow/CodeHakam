namespace ContentService.Tests.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Implementations;
using ContentService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class TestCaseServiceTests
{
    private readonly Mock<ITestCaseRepository> _testCaseRepositoryMock;
    private readonly Mock<IProblemRepository> _problemRepositoryMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<TestCaseService>> _loggerMock;
    private readonly TestCaseService _service;

    public TestCaseServiceTests()
    {
        _testCaseRepositoryMock = new Mock<ITestCaseRepository>();
        _problemRepositoryMock = new Mock<IProblemRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<TestCaseService>>();

        _configurationMock
            .Setup(c => c["ContentService:MaxTestCaseFileSize"])
            .Returns("10485760");

        _configurationMock
            .Setup(c => c["MinIO:BucketName"])
            .Returns("codehakam-testcases");

        _service = new TestCaseService(
            _testCaseRepositoryMock.Object,
            _problemRepositoryMock.Object,
            _storageServiceMock.Object,
            _eventPublisherMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetTestCaseAsync_WithValidId_ShouldReturnTestCase()
    {
        var testCaseId = 1L;
        var expectedTestCase = new TestCase
        {
            Id = testCaseId,
            ProblemId = 10L,
            InputFileUrl = "path/to/input.txt",
            OutputFileUrl = "path/to/output.txt",
            IsSample = true
        };

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync(expectedTestCase);

        var result = await _service.GetTestCaseAsync(testCaseId);

        Assert.NotNull(result);
        Assert.Equal(testCaseId, result.Id);
        Assert.Equal(10L, result.ProblemId);
    }

    [Fact]
    public async Task GetTestCaseAsync_WithInvalidId_ShouldReturnNull()
    {
        var testCaseId = 999L;

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync((TestCase?)null);

        var result = await _service.GetTestCaseAsync(testCaseId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTestCasesAsync_WithSamplesOnly_ShouldReturnOnlySampleTestCases()
    {
        var problemId = 10L;
        var sampleTestCases = new List<TestCase>
        {
            new TestCase { Id = 1, ProblemId = problemId, InputFileUrl = "input1.txt", OutputFileUrl = "output1.txt", IsSample = true },
            new TestCase { Id = 2, ProblemId = problemId, InputFileUrl = "input2.txt", OutputFileUrl = "output2.txt", IsSample = true }
        };

        _testCaseRepositoryMock
            .Setup(r => r.GetSampleTestCasesAsync(problemId))
            .ReturnsAsync(sampleTestCases);

        var result = await _service.GetTestCasesAsync(problemId, samplesOnly: true);

        Assert.Equal(2, result.Count());
        Assert.All(result, tc => Assert.True(tc.IsSample));
    }

    [Fact]
    public async Task GetTestCasesAsync_WithoutSamplesOnly_ShouldReturnAllTestCases()
    {
        var problemId = 10L;
        var allTestCases = new List<TestCase>
        {
            new TestCase { Id = 1, ProblemId = problemId, InputFileUrl = "input1.txt", OutputFileUrl = "output1.txt", IsSample = true },
            new TestCase { Id = 2, ProblemId = problemId, InputFileUrl = "input2.txt", OutputFileUrl = "output2.txt", IsSample = false },
            new TestCase { Id = 3, ProblemId = problemId, InputFileUrl = "input3.txt", OutputFileUrl = "output3.txt", IsSample = false }
        };

        _testCaseRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(allTestCases);

        var result = await _service.GetTestCasesAsync(problemId, samplesOnly: false);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetTestCaseCountAsync_ShouldReturnCount()
    {
        var problemId = 10L;

        _testCaseRepositoryMock
            .Setup(r => r.GetCountByProblemAsync(problemId))
            .ReturnsAsync(5);

        var result = await _service.GetTestCaseCountAsync(problemId);

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithValidData_ShouldUploadAndCreateTestCase()
    {
        var problemId = 10L;
        var userId = 100L;
        var inputData = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("1 2"));
        var outputData = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("3"));
        var inputSize = 3L;
        var outputSize = 1L;
        var isSample = true;
        var orderIndex = 1;

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = userId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        _storageServiceMock
            .Setup(s => s.EnsureBucketExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string bucket, string path, Stream data, long size, string contentType, CancellationToken ct) => path);

        _testCaseRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<TestCase>()))
            .ReturnsAsync((TestCase tc) =>
            {
                tc.Id = 1L;
                return tc;
            });

        _eventPublisherMock
            .Setup(e => e.PublishTestCaseUploadedAsync(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UploadTestCaseAsync(
            problemId,
            inputData,
            outputData,
            inputSize,
            outputSize,
            isSample,
            orderIndex,
            userId);

        Assert.NotNull(result);
        Assert.Equal(problemId, result.ProblemId);
        Assert.Equal(isSample, result.IsSample);
        Assert.Equal(orderIndex, result.TestNumber);
        Assert.Equal(inputSize, result.InputSize);
        Assert.Equal(outputSize, result.OutputSize);

        _storageServiceMock.Verify(
            s => s.UploadFileAsync(
                "codehakam-testcases",
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<long>(),
                "text/plain",
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _testCaseRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<TestCase>()), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishTestCaseUploadedAsync(problemId, It.IsAny<long>(), isSample, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var problemId = 999L;
        var userId = 100L;
        var inputData = new MemoryStream();
        var outputData = new MemoryStream();

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync((Problem?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UploadTestCaseAsync(
                problemId,
                inputData,
                outputData,
                100L,
                100L,
                true,
                1,
                userId));
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var problemId = 10L;
        var authorId = 100L;
        var differentUserId = 200L;
        var inputData = new MemoryStream();
        var outputData = new MemoryStream();

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = authorId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.UploadTestCaseAsync(
                problemId,
                inputData,
                outputData,
                100L,
                100L,
                true,
                1,
                differentUserId));
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithExcessiveInputSize_ShouldThrowException()
    {
        var problemId = 10L;
        var userId = 100L;
        var inputData = new MemoryStream();
        var outputData = new MemoryStream();
        var excessiveSize = 11000000L;

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = userId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UploadTestCaseAsync(
                problemId,
                inputData,
                outputData,
                excessiveSize,
                100L,
                true,
                1,
                userId));
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithExcessiveOutputSize_ShouldThrowException()
    {
        var problemId = 10L;
        var userId = 100L;
        var inputData = new MemoryStream();
        var outputData = new MemoryStream();
        var excessiveSize = 11000000L;

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = userId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UploadTestCaseAsync(
                problemId,
                inputData,
                outputData,
                100L,
                excessiveSize,
                true,
                1,
                userId));
    }

    [Fact]
    public async Task DeleteTestCaseAsync_WithValidData_ShouldDeleteTestCaseAndFiles()
    {
        var testCaseId = 1L;
        var problemId = 10L;
        var userId = 100L;

        var testCase = new TestCase
        {
            Id = testCaseId,
            ProblemId = problemId,
            InputFileUrl = "path/to/input.txt",
            OutputFileUrl = "path/to/output.txt",
            IsSample = false
        };

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = userId
        };

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync(testCase);

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        _storageServiceMock
            .Setup(s => s.DeleteFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _testCaseRepositoryMock
            .Setup(r => r.DeleteAsync(testCaseId))
            .ReturnsAsync(true);

        await _service.DeleteTestCaseAsync(testCaseId, userId);

        _storageServiceMock.Verify(
            s => s.DeleteFileAsync("codehakam-testcases", testCase.InputFileUrl, It.IsAny<CancellationToken>()),
            Times.Once);

        _storageServiceMock.Verify(
            s => s.DeleteFileAsync("codehakam-testcases", testCase.OutputFileUrl, It.IsAny<CancellationToken>()),
            Times.Once);

        _testCaseRepositoryMock.Verify(r => r.DeleteAsync(testCaseId), Times.Once);
    }

    [Fact]
    public async Task DeleteTestCaseAsync_WithNonExistentTestCase_ShouldThrowException()
    {
        var testCaseId = 999L;
        var userId = 100L;

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync((TestCase?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.DeleteTestCaseAsync(testCaseId, userId));
    }

    [Fact]
    public async Task DeleteTestCaseAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var testCaseId = 1L;
        var problemId = 10L;
        var authorId = 100L;
        var differentUserId = 200L;

        var testCase = new TestCase
        {
            Id = testCaseId,
            ProblemId = problemId,
            InputFileUrl = "path/to/input.txt",
            OutputFileUrl = "path/to/output.txt",
            IsSample = false
        };

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input format",
            OutputFormat = "Output format",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = authorId
        };

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync(testCase);

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.DeleteTestCaseAsync(testCaseId, differentUserId));
    }

    [Fact]
    public async Task DownloadTestCaseInputAsync_WithValidId_ShouldReturnStream()
    {
        var testCaseId = 1L;
        var testCase = new TestCase
        {
            Id = testCaseId,
            ProblemId = 10L,
            InputFileUrl = "path/to/input.txt",
            OutputFileUrl = "path/to/output.txt",
            IsSample = true
        };

        var expectedStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test data"));

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync(testCase);

        _storageServiceMock
            .Setup(s => s.DownloadFileAsync(
                "codehakam-testcases",
                testCase.InputFileUrl,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var result = await _service.DownloadTestCaseInputAsync(testCaseId);

        Assert.NotNull(result);
        Assert.Equal(expectedStream, result);
    }

    [Fact]
    public async Task DownloadTestCaseInputAsync_WithNonExistentTestCase_ShouldThrowException()
    {
        var testCaseId = 999L;

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync((TestCase?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.DownloadTestCaseInputAsync(testCaseId));
    }

    [Fact]
    public async Task DownloadTestCaseOutputAsync_WithValidId_ShouldReturnStream()
    {
        var testCaseId = 1L;
        var testCase = new TestCase
        {
            Id = testCaseId,
            ProblemId = 10L,
            InputFileUrl = "path/to/input.txt",
            OutputFileUrl = "path/to/output.txt",
            IsSample = true
        };

        var expectedStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("expected output"));

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync(testCase);

        _storageServiceMock
            .Setup(s => s.DownloadFileAsync(
                "codehakam-testcases",
                testCase.OutputFileUrl,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var result = await _service.DownloadTestCaseOutputAsync(testCaseId);

        Assert.NotNull(result);
        Assert.Equal(expectedStream, result);
    }

    [Fact]
    public async Task DownloadTestCaseOutputAsync_WithNonExistentTestCase_ShouldThrowException()
    {
        var testCaseId = 999L;

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync((TestCase?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.DownloadTestCaseOutputAsync(testCaseId));
    }

    [Fact]
    public async Task ValidateTestCaseSizeAsync_WithValidSize_ShouldReturnTrue()
    {
        var validSize = 5000000L;

        var result = await _service.ValidateTestCaseSizeAsync(validSize);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTestCaseSizeAsync_WithExcessiveSize_ShouldReturnFalse()
    {
        var excessiveSize = 11000000L;

        var result = await _service.ValidateTestCaseSizeAsync(excessiveSize);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTestCaseSizeAsync_WithZeroSize_ShouldReturnFalse()
    {
        var zeroSize = 0L;

        var result = await _service.ValidateTestCaseSizeAsync(zeroSize);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTestCaseSizeAsync_WithNegativeSize_ShouldReturnFalse()
    {
        var negativeSize = -100L;

        var result = await _service.ValidateTestCaseSizeAsync(negativeSize);

        Assert.False(result);
    }
}
