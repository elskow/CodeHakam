using System.Text;
using ContentService.Enums;
using ContentService.Models;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Implementations;
using ContentService.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContentService.Tests.Services;

public class TestCaseServiceTests
{
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IProblemRepository> _problemRepositoryMock;
    private readonly TestCaseService _service;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<ITestCaseRepository> _testCaseRepositoryMock;

    public TestCaseServiceTests()
    {
        _testCaseRepositoryMock = new Mock<ITestCaseRepository>();
        _problemRepositoryMock = new Mock<IProblemRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        var configurationMock = new Mock<IConfiguration>();
        var loggerMock = new Mock<ILogger<TestCaseService>>();

        configurationMock
            .Setup(c => c["ContentService:MaxTestCaseFileSize"])
            .Returns("10485760");

        configurationMock
            .Setup(c => c["MinIO:BucketName"])
            .Returns("codehakam-testcases");

        _service = new TestCaseService(
            _testCaseRepositoryMock.Object,
            _problemRepositoryMock.Object,
            _storageServiceMock.Object,
            _eventPublisherMock.Object,
            configurationMock.Object,
            loggerMock.Object);
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
            IsSample = true,
            TestNumber = 1
        };

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync(expectedTestCase);

        var result = await _service.GetTestCaseAsync(testCaseId);

        Assert.NotNull(result);
        Assert.Equal(testCaseId, result.Id);
        Assert.Equal(expected: 10L, result.ProblemId);
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
            new()
                { Id = 1, ProblemId = problemId, InputFileUrl = "input1.txt", OutputFileUrl = "output1.txt", IsSample = true, TestNumber = 1 },
            new()
                { Id = 2, ProblemId = problemId, InputFileUrl = "input2.txt", OutputFileUrl = "output2.txt", IsSample = true, TestNumber = 2 }
        };

        _testCaseRepositoryMock
            .Setup(r => r.GetSampleTestCasesAsync(problemId))
            .ReturnsAsync(sampleTestCases);

        var result = await _service.GetTestCasesAsync(problemId, samplesOnly: true);

        IEnumerable<TestCase> testCases = result as TestCase[] ?? result.ToArray();
        Assert.Equal(expected: 2, testCases.Count());
        Assert.All(testCases, tc => Assert.True(tc.IsSample));
    }

    [Fact]
    public async Task GetTestCasesAsync_WithoutSamplesOnly_ShouldReturnAllTestCases()
    {
        var problemId = 10L;
        var allTestCases = new List<TestCase>
        {
            new()
                { Id = 1, ProblemId = problemId, InputFileUrl = "input1.txt", OutputFileUrl = "output1.txt", IsSample = true, TestNumber = 1 },
            new()
                { Id = 2, ProblemId = problemId, InputFileUrl = "input2.txt", OutputFileUrl = "output2.txt", IsSample = false, TestNumber = 2 },
            new()
                { Id = 3, ProblemId = problemId, InputFileUrl = "input3.txt", OutputFileUrl = "output3.txt", IsSample = false, TestNumber = 3 }
        };

        _testCaseRepositoryMock
            .Setup(r => r.GetByProblemIdAsync(problemId))
            .ReturnsAsync(allTestCases);

        var result = await _service.GetTestCasesAsync(problemId, samplesOnly: false);

        Assert.Equal(expected: 3, result.Count());
    }

    [Fact]
    public async Task GetTestCaseCountAsync_ShouldReturnCount()
    {
        var problemId = 10L;

        _testCaseRepositoryMock
            .Setup(r => r.GetCountByProblemAsync(problemId))
            .ReturnsAsync(5);

        var result = await _service.GetTestCaseCountAsync(problemId);

        Assert.Equal(expected: 5, result);
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithValidData_ShouldUploadAndCreateTestCase()
    {
        var problemId = 1L;
        var userId = 100L;
        var isSample = true;
        var testNumber = 1;
        var inputSize = 1024L;
        var outputSize = 512L;

        var inputFileMock = new Mock<IFormFile>();
        inputFileMock.Setup(f => f.Length).Returns(inputSize);
        inputFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        var outputFileMock = new Mock<IFormFile>();
        outputFileMock.Setup(f => f.Length).Returns(outputSize);
        outputFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = userId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("uploaded-object-path");

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
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UploadTestCaseAsync(
            problemId,
            inputFileMock.Object,
            outputFileMock.Object,
            isSample,
            testNumber,
            userId);

        Assert.NotNull(result);
        Assert.Equal(problemId, result.ProblemId);
        Assert.Equal(isSample, result.IsSample);
        Assert.Equal(testNumber, result.TestNumber);

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
            e => e.PublishTestCaseUploadedAsync(It.IsAny<long>(), problemId, testNumber, isSample, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithNonExistentProblem_ShouldThrowException()
    {
        var problemId = 999L;
        var userId = 100L;

        var inputFileMock = new Mock<IFormFile>();
        inputFileMock.Setup(f => f.Length).Returns(1024L);

        var outputFileMock = new Mock<IFormFile>();
        outputFileMock.Setup(f => f.Length).Returns(512L);

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync((Problem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.UploadTestCaseAsync(problemId, inputFileMock.Object, outputFileMock.Object, isSample: true, testNumber: 1, userId));
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        var problemId = 1L;
        var authorId = 100L;
        var differentUserId = 200L;

        var inputFileMock = new Mock<IFormFile>();
        inputFileMock.Setup(f => f.Length).Returns(1024L);

        var outputFileMock = new Mock<IFormFile>();
        outputFileMock.Setup(f => f.Length).Returns(512L);

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = authorId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.UploadTestCaseAsync(problemId, inputFileMock.Object, outputFileMock.Object, isSample: true, testNumber: 1, differentUserId));
    }

    [Fact]
    public async Task UploadTestCaseAsync_WithExcessiveFileSize_ShouldThrowException()
    {
        var problemId = 1L;
        var userId = 100L;
        var excessiveSize = 11 * 1024 * 1024L; // 11 MB

        var inputFileMock = new Mock<IFormFile>();
        inputFileMock.Setup(f => f.Length).Returns(excessiveSize);

        var outputFileMock = new Mock<IFormFile>();
        outputFileMock.Setup(f => f.Length).Returns(512L);

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
            Constraints = "Constraints",
            Difficulty = Difficulty.Easy,
            AuthorId = userId
        };

        _problemRepositoryMock
            .Setup(r => r.GetByIdAsync(problemId, false))
            .ReturnsAsync(problem);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UploadTestCaseAsync(problemId, inputFileMock.Object, outputFileMock.Object, isSample: true, testNumber: 1, userId));
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
            InputFileUrl = "codehakam-testcases/problem-10/test-1/input.txt",
            OutputFileUrl = "codehakam-testcases/problem-10/test-1/output.txt",
            IsSample = true,
            TestNumber = 1
        };

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
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
            .Setup(s => s.FileExistsAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(true);

        _storageServiceMock
            .Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        _testCaseRepositoryMock
            .Setup(r => r.DeleteAsync(testCaseId))
            .ReturnsAsync(true);

        _eventPublisherMock
            .Setup(e => e.PublishTestCaseDeletedAsync(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        await _service.DeleteTestCaseAsync(testCaseId, userId);

        _storageServiceMock.Verify(
            s => s.DeleteFileAsync("codehakam-testcases", It.IsAny<string>(), CancellationToken.None),
            Times.Exactly(2));

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

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
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
            IsSample = true,
            TestNumber = 1
        };

        var problem = new Problem
        {
            Id = problemId,
            Title = "Test Problem",
            Slug = "test-problem",
            Description = "Description",
            InputFormat = "Input",
            OutputFormat = "Output",
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
            InputFileUrl = "codehakam-testcases/problem-10/test-1/input.txt",
            OutputFileUrl = "codehakam-testcases/problem-10/test-1/output.txt",
            IsSample = true,
            TestNumber = 1
        };

        var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("test input"));

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync(testCase);

        _storageServiceMock
            .Setup(s => s.DownloadFileAsync(
                "codehakam-testcases",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var (stream, contentType, fileName) = await _service.DownloadTestCaseInputAsync(testCaseId);

        Assert.NotNull(stream);
        Assert.Equal(expectedStream, stream);
        Assert.Equal("text/plain", contentType);
        Assert.NotNull(fileName);
        Assert.Contains("input", fileName);
    }

    [Fact]
    public async Task DownloadTestCaseInputAsync_WithNonExistentTestCase_ShouldThrowException()
    {
        var testCaseId = 999L;

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync((TestCase?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
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
            InputFileUrl = "codehakam-testcases/problem-10/test-1/input.txt",
            OutputFileUrl = "codehakam-testcases/problem-10/test-1/output.txt",
            IsSample = true,
            TestNumber = 1
        };

        var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("expected output"));

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync(testCase);

        _storageServiceMock
            .Setup(s => s.DownloadFileAsync(
                "codehakam-testcases",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var (stream, contentType, fileName) = await _service.DownloadTestCaseOutputAsync(testCaseId);

        Assert.NotNull(stream);
        Assert.Equal(expectedStream, stream);
        Assert.Equal("text/plain", contentType);
        Assert.NotNull(fileName);
        Assert.Contains("output", fileName);
    }

    [Fact]
    public async Task DownloadTestCaseOutputAsync_WithNonExistentTestCase_ShouldThrowException()
    {
        var testCaseId = 999L;

        _testCaseRepositoryMock
            .Setup(r => r.GetByIdAsync(testCaseId))
            .ReturnsAsync((TestCase?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.DownloadTestCaseOutputAsync(testCaseId));
    }

    [Fact]
    public async Task ValidateTestCaseSizeAsync_WithValidSize_ShouldReturnTrue()
    {
        var validSize = 1024L;

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
