namespace anotterwebpage_WebApi.Api.Errors;

public static class ApiErrorResults
{
    public static IResult Validation(
        Dictionary<string, string[]> errors)
    {
        return Results.ValidationProblem(
            errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Validation Failed",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        );
    }

    public static IResult NotFound(string message)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource Not Found",
            detail: message
        );
    }

    public static IResult BusinessViolation(string message)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Business Rule Violation",
            detail: message
        );
    }

    public static IResult InternalError()
    {
        return Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error",
            detail: "An unexpected error occurred."
        );
    }
}