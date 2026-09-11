using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Api.Requests;

namespace anotterwebpage_WebApi.Api.Endpoints;

public static class CollaborationEndpoints
{
    public static RouteGroupBuilder MapCollaborationEndpoints(
        this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/collaborations")
            .WithTags("Collaborations");

        group.MapPost("/", Create);

        return group;
    }

    private static async Task<IResult> Create(
        CreateCollabRequest request,
        ICollabDelegate collaborationDelegate)
    {
        var collaboration =
            await collaborationDelegate.CreateAsync(request);

        return Results.Created(
            $"/api/collaborations/{collaboration.Id}",
            collaboration);
    }
}