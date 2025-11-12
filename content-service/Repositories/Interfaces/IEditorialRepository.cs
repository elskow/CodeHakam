using ContentService.Models;

namespace ContentService.Repositories.Interfaces;

public interface IEditorialRepository
{
    Task<Editorial?> GetByIdAsync(long id);
    Task<Editorial?> GetByProblemIdAsync(long problemId);
    Task<IEnumerable<Editorial>> GetPublishedEditorialsAsync(int page, int pageSize);
    Task<Editorial> CreateAsync(Editorial editorial);
    Task<Editorial> UpdateAsync(Editorial editorial);
    Task<bool> DeleteAsync(long id);
    Task<bool> PublishAsync(long id);
    Task<bool> UnpublishAsync(long id);
    Task<bool> ExistsForProblemAsync(long problemId);
    Task<int> GetPublishedCountAsync();
}
