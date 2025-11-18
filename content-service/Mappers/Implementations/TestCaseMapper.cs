using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Mappers.Interfaces;

namespace ContentService.Mappers.Implementations;

public class TestCaseMapper : ITestCaseMapper
{
    public TestCaseResponse ToResponse(TestCase testCase)
    {
        return new TestCaseResponse
        {
            Id = testCase.Id,
            ProblemId = testCase.ProblemId,
            TestNumber = testCase.TestNumber,
            IsSample = testCase.IsSample,
            InputFileUrl = testCase.InputFileUrl,
            OutputFileUrl = testCase.OutputFileUrl,
            CreatedAt = testCase.CreatedAt
        };
    }

    public List<TestCaseResponse> ToResponses(IEnumerable<TestCase> testCases)
    {
        return testCases.Select(ToResponse).ToList();
    }
}