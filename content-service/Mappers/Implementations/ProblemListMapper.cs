using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Mappers.Interfaces;

namespace ContentService.Mappers.Implementations;

public class ProblemListMapper : IProblemListMapper
{
    public ProblemListResponse ToResponse(ProblemList list)
    {
        return new ProblemListResponse
        {
            Id = list.Id,
            Name = list.Title,
            Description = list.Description,
            OwnerId = list.OwnerId,
            IsPublic = list.IsPublic,
            ProblemCount = list.ProblemIds?.Length ?? 0,
            CreatedAt = list.CreatedAt,
            UpdatedAt = list.UpdatedAt
        };
    }

    public ProblemListResponse ToResponseWithProblems(ProblemList list)
    {
        return new ProblemListResponse
        {
            Id = list.Id,
            Name = list.Title,
            Description = list.Description,
            OwnerId = list.OwnerId,
            IsPublic = list.IsPublic,
            ProblemCount = list.ProblemIds?.Length ?? 0,
            CreatedAt = list.CreatedAt,
            UpdatedAt = list.UpdatedAt
        };
    }

    public List<ProblemListResponse> ToResponses(IEnumerable<ProblemList> lists)
    {
        return lists.Select(ToResponse).ToList();
    }
}