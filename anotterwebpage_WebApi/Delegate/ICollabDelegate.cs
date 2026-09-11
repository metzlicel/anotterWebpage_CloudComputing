using anotterwebpage_WebApi.Api.Requests;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Delegate;

public interface ICollabDelegate
{
    Task<Collab> CreateAsync(CreateCollabRequest request);
    Task<List<Collab>> GetAllAsync();
    Task<Collab?> GetByIdAsync(int id);
    Task<Collab?> UpdateAsync(
        int id,
        UpdateCollabRequest request);
    Task<bool> DeleteAsync(int id);
}