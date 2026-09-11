using FluentValidation;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Api.Extensions;
using anotterwebpage_WebApi.Api.Errors;

namespace anotterwebpage_WebApi.Api.Routes;

public static class CollaborationRoutes
{
    public static RouteGroupBuilder MapCollaborationRoutes(
        this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/collaborations")
            .WithTags("Collaborations");

        group.MapPost("/", Create);
        group.MapGet("/", GetAll);
        group.MapGet("/{id}", GetById);
        group.MapPut("/{id}", Update);
        group.MapDelete("/{id}", Delete);
        
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

            return ApiErrorResults.Validation(errors);
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
        string id,
        ICollabDelegate collabDelegate)
    {
        if (!int.TryParse(id, out var collabId))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["id"] = new[]
                {
                    "El formato del ID no es válido."
                }
            };

            return ApiErrorResults.Validation(errors);
        }

        var collab =
            await collabDelegate.GetByIdAsync(collabId);

        if (collab == null)
        {
            return ApiErrorResults.NotFound(
                "Collaboration not found.");
        }

        return Results.Ok(
            collab.ToDto());
    }
    
    private static async Task<IResult> Update(
        string id,
        UpdateCollabDto request,
        ICollabDelegate collabDelegate,
        IValidator<UpdateCollabDto> validator)
    {
        if (!int.TryParse(id, out var collabId))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["id"] = new[]
                {
                    "El formato del ID no es válido."
                }
            };

            return ApiErrorResults.Validation(errors);
        }

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
            await collabDelegate.UpdateAsync(
                collabId,
                request);

        if (collaboration == null)
        {
            return ApiErrorResults.NotFound(
                "Collaboration not found.");
        }

        return Results.Ok(
            collaboration.ToDto());
    }
    
    private static async Task<IResult> Delete(
        string id,
        ICollabDelegate collabDelegate)
    {
        if (!int.TryParse(id, out var collabId))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["id"] = new[]
                {
                    "El formato del ID no es válido."
                }
            };

            return ApiErrorResults.Validation(errors);
        }

        var deleted =
            await collabDelegate.DeleteAsync(collabId);

        if (!deleted)
        {
            return ApiErrorResults.NotFound(
                "Collaboration not found.");
        }

        return Results.NoContent();
    }
}