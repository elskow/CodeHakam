using ContentService.DTOs.Responses;
using ContentService.Models;

namespace ContentService.Mappers.Interfaces;

public interface ITestCaseMapper
{
    TestCaseResponse ToResponse(TestCase testCase);
    List<TestCaseResponse> ToResponses(IEnumerable<TestCase> testCases);
}