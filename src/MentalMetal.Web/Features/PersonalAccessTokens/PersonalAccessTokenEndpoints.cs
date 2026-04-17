using MentalMetal.Application.PersonalAccessTokens;
using MentalMetal.Domain.Common;

namespace MentalMetal.Web.Features.PersonalAccessTokens;

public static class PersonalAccessTokenEndpoints
{
    public static IEndpointRouteBuilder MapPersonalAccessTokenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/personal-access-tokens", async (
            CreatePatRequest request,
            CreatePersonalAccessTokenHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return Results.Created($"/api/personal-access-tokens/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        app.MapGet("/api/personal-access-tokens", async (
            ListPersonalAccessTokensHandler handler,
            CancellationToken cancellationToken) =>
        {
            var tokens = await handler.HandleAsync(cancellationToken);
            return Results.Ok(tokens);
        }).RequireAuthorization();

        app.MapPost("/api/personal-access-tokens/{id}/revoke", async (
            Guid id,
            RevokePersonalAccessTokenHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await handler.HandleAsync(id, cancellationToken);
                return Results.NoContent();
            }
            catch (NotFoundException)
            {
                return Results.NotFound();
            }
        }).RequireAuthorization();

        return app;
    }
}
