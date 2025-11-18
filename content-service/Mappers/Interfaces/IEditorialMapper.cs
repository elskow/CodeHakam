using ContentService.DTOs.Responses;
using ContentService.Models;

namespace ContentService.Mappers.Interfaces;

public interface IEditorialMapper
{
    EditorialResponse ToResponse(Editorial editorial);
}