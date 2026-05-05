namespace DrugRegistry.API.Endpoints.V2;

internal static class V2ProblemResponses
{
    public static IResult BadRequest(string detail)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid request",
            detail: detail,
            type: "https://httpstatuses.com/400"
        );
    }

    public static IResult NotFound(string detail)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource not found",
            detail: detail,
            type: "https://httpstatuses.com/404"
        );
    }
}