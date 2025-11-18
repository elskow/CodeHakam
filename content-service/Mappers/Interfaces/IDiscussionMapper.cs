using ContentService.DTOs.Responses;
using ContentService.Models;

namespace ContentService.Mappers.Interfaces;

public interface IDiscussionMapper
{
    DiscussionResponse ToResponse(Discussion discussion);
    DiscussionResponse ToResponseWithComments(Discussion discussion);
    List<DiscussionResponse> ToResponses(IEnumerable<Discussion> discussions);
    CommentResponse ToCommentResponse(DiscussionComment comment);
}