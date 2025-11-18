using ContentService.DTOs.Common;
using ContentService.DTOs.Responses;
using ContentService.Mappers.Interfaces;
using ContentService.Models;

namespace ContentService.Mappers.Implementations;

public class ProblemMapper : IProblemMapper
{
    public ProblemResponse ToResponse(Problem problem, Dictionary<long, UserProfile> authorProfiles)
    {
        authorProfiles.TryGetValue(problem.AuthorId, out var authorProfile);

        return new ProblemResponse
        {
            Id = problem.Id,
            Title = problem.Title,
            Slug = problem.Slug,
            Description = problem.Description,
            InputFormat = problem.InputFormat,
            OutputFormat = problem.OutputFormat,
            Constraints = problem.Constraints,
            Difficulty = problem.Difficulty.ToString(),
            TimeLimit = problem.TimeLimit,
            MemoryLimit = problem.MemoryLimit,
            AuthorId = problem.AuthorId,
            AuthorName = authorProfile?.DisplayName,
            AuthorAvatar = authorProfile?.AvatarUrl,
            Visibility = problem.Visibility.ToString(),
            HintText = problem.HintText,
            Tags = problem.Tags.Select(t => t.Tag).ToList(),
            ViewCount = problem.ViewCount,
            SubmissionCount = problem.SubmissionCount,
            AcceptedCount = problem.AcceptedCount,
            AcceptanceRate = problem.SubmissionCount > 0
                ? Math.Round((double)problem.AcceptedCount / problem.SubmissionCount * 100, digits: 2)
                : 0.0,
            CreatedAt = problem.CreatedAt,
            UpdatedAt = problem.UpdatedAt
        };
    }

    public List<ProblemResponse> ToResponses(IEnumerable<Problem> problems, Dictionary<long, UserProfile> authorProfiles)
    {
        return problems.Select(p => ToResponse(p, authorProfiles)).ToList();
    }

    public PagedResponse<ProblemResponse> ToPagedResponse(
        IEnumerable<Problem> problems,
        Dictionary<long, UserProfile> authorProfiles,
        int page,
        int pageSize,
        int totalCount)
    {
        return new PagedResponse<ProblemResponse>
        {
            Items = ToResponses(problems, authorProfiles),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
