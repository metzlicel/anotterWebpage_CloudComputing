using FluentValidation;
using anotterwebpage_WebApi.Api.Requests;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Api.Errors;

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
        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
        
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
    
    private static async Task<IResult> GetAll(
        ICollabDelegate collabDelegate)
    {
        var collaborations =
            await collabDelegate.GetAllAsync();

        return Results.Ok(collaborations);
    }
    
    private static async Task<IResult> GetById(
        int id,
        ICollabDelegate collabDelegate)
    {
        var collaboration =
            await collabDelegate.GetByIdAsync(id);

        if (collaboration == null)
        {
            return ApiErrorResults.NotFound(
                "Collaboration not found.");
        }

        return Results.Ok(collaboration);
    }
    
    private static async Task<IResult> Update(
        int id,
        UpdateCollabRequest request,
        ICollabDelegate collabDelegate,
        IValidator<UpdateCollabRequest> validator)
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

            return ApiErrorResults.Validation(errors);
        }

        var collab =
            await collabDelegate.UpdateAsync(id, request);

        if (collab == null)
        {
            return ApiErrorResults.NotFound(
                "Collaboration not found.");
        }

        return Results.Ok(collab);
    }
    
    private static async Task<IResult> Delete(
        int id,
        ICollabDelegate collabDelegate)
    {
        var deleted =
            await collabDelegate.DeleteAsync(id);

        if (!deleted)
        {
            return ApiErrorResults.NotFound(
                "Collaboration not found.");
        }

        return Results.NoContent();
    }
}