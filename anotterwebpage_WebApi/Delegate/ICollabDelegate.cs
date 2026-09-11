using anotterwebpage_WebApi.Api.Requests;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Delegate;

public interface ICollabDelegate
{
    Task<Collab> CreateAsync(CreateCollabRequest request);
}