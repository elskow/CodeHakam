using ContentService.DTOs.Responses;
using ContentService.Models;
using ContentService.Mappers.Interfaces;

namespace ContentService.Mappers.Implementations;

public class EditorialMapper : IEditorialMapper
{
    public EditorialResponse ToResponse(Editorial editorial)
    {
        return new EditorialResponse
        {
            Id = editorial.Id,
            ProblemId = editorial.ProblemId,
            AuthorId = editorial.AuthorId,
            Content = editorial.Content,
            TimeComplexity = editorial.TimeComplexity,
            SpaceComplexity = editorial.SpaceComplexity,
            VideoUrl = editorial.VideoUrl,
            IsPublished = editorial.IsPublished,
            CreatedAt = editorial.CreatedAt,
            UpdatedAt = editorial.UpdatedAt
        };
    }
}