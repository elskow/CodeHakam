using AccountService.DTOs;
using AccountService.DTOs.Common;
using AccountService.Utilities;

namespace AccountService.Extensions;

public static class DtoExtensions
{
    public static UserSearchRequest Normalize(this UserSearchRequest request)
    {
        return request with
        {
            SearchTerm = InputSanitizer.SanitizeSearchTerm(request.SearchTerm),
            Country = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim().ToUpperInvariant(),
            SortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "rating" : request.SortBy.Trim().ToLowerInvariant(),
            SortOrder = string.IsNullOrWhiteSpace(request.SortOrder) ? "desc" : request.SortOrder.Trim().ToLowerInvariant()
        };
    }


}
