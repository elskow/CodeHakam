using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Mappers.Interfaces;

namespace ContentService.Mappers.Implementations;

public class DiscussionMapper : IDiscussionMapper
{
    public DiscussionResponse ToResponse(Discussion discussion)
    {
        return new DiscussionResponse
        {
            Id = discussion.Id,
            ProblemId = discussion.ProblemId,
            UserId = discussion.UserId,
            Title = discussion.Title,
            Content = discussion.Content,
            VoteCount = discussion.VoteCount,
            CommentCount = discussion.Comments?.Count ?? 0,
            CreatedAt = discussion.CreatedAt,
            UpdatedAt = discussion.UpdatedAt
        };
    }

    public DiscussionResponse ToResponseWithComments(Discussion discussion)
    {
        return new DiscussionResponse
        {
            Id = discussion.Id,
            ProblemId = discussion.ProblemId,
            UserId = discussion.UserId,
            Title = discussion.Title,
            Content = discussion.Content,
            VoteCount = discussion.VoteCount,
            CommentCount = discussion.Comments?.Count ?? 0,
            CreatedAt = discussion.CreatedAt,
            UpdatedAt = discussion.UpdatedAt,
            Comments = discussion.Comments?.Select(ToCommentResponse).ToList() ?? new List<CommentResponse>()
        };
    }

    public List<DiscussionResponse> ToResponses(IEnumerable<Discussion> discussions)
    {
        return discussions.Select(ToResponse).ToList();
    }

    public CommentResponse ToCommentResponse(DiscussionComment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            DiscussionId = comment.DiscussionId,
            UserId = comment.UserId,
            ParentId = comment.ParentId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }
}