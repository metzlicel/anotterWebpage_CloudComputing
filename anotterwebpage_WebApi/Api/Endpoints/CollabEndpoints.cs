using FluentValidation;
using anotterwebpage_WebApi.Api.Requests;
using anotterwebpage_WebApi.Delegate;

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
        ICollabDelegate collaborationDelegate,
        IValidator<CreateCollabRequest> validator)
    {
        var validationResult =
            await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        var collaboration =
            await collaborationDelegate.CreateAsync(request);

        return Results.Created(
            $"/api/collaborations/{collaboration.Id}",
            collaboration);
    }
}