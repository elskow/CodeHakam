using ContentService.DTOs.Responses;
using ContentService.Models;

namespace ContentService.Mappers.Interfaces;

public interface IProblemListMapper
{
    ProblemListResponse ToResponse(ProblemList list);
    ProblemListResponse ToResponseWithProblems(ProblemList list);
    List<ProblemListResponse> ToResponses(IEnumerable<ProblemList> lists);
}