using FluentValidation;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Api.Extensions;
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
        CreateCollabDto request,
        ICollabDelegate collaborationDelegate,
        IValidator<CreateCollabDto> validator)
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
            collaboration.ToDto());
    }
    
    private static async Task<IResult> GetAll(
        ICollabDelegate collabDelegate)
    {
        var collaborations =
            await collabDelegate.GetAllAsync();

        return Results.Ok(
            collaborations.Select(c => c.ToDto()).ToList());
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

        return Results.Ok(collaboration.ToDto());
    }
    
    private static async Task<IResult> Update(
        int id,
        UpdateCollabDto request,
        ICollabDelegate collabDelegate,
        IValidator<UpdateCollabDto> validator)
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

        var collaboration =
            await collabDelegate.UpdateAsync(id, request);

        if (collaboration == null)
        {
            return ApiErrorResults.NotFound(
                "Collaboration not found.");
        }

        return Results.Ok(collaboration.ToDto());
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