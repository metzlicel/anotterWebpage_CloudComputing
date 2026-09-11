using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public interface ICollabRepository
{
    Task<Collab> CreateAsync(Collab collaboration);
    Task<List<Collab>> GetAllAsync();
    Task<Collab?> GetByIdAsync(int id);
    Task<Collab> UpdateAsync(Collab collab);
    Task DeleteAsync(Collab collab);
}