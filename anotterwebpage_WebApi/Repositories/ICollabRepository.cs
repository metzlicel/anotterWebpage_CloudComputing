using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public interface ICollabRepository
{
    Task<Collab> CreateAsync(Collab collaboration);
}