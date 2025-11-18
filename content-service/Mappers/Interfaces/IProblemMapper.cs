using ContentService.DTOs.Common;
using ContentService.DTOs.Responses;
using ContentService.Models;

namespace ContentService.Mappers.Interfaces;

public interface IProblemMapper
{
    ProblemResponse ToResponse(Problem problem, Dictionary<long, UserProfile> authorProfiles);
    List<ProblemResponse> ToResponses(IEnumerable<Problem> problems, Dictionary<long, UserProfile> authorProfiles);
    PagedResponse<ProblemResponse> ToPagedResponse(
        IEnumerable<Problem> problems,
        Dictionary<long, UserProfile> authorProfiles,
        int page,
        int pageSize,
        int totalCount);
}
