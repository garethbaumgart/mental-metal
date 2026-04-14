using MentalMetal.Application.Briefings;

namespace MentalMetal.Web;

public static class BriefingEndpoints
{
    public static IEndpointRouteBuilder MapBriefingEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/briefings/morning?force=true|false
        app.MapPost("/api/briefings/morning", async (
            HttpContext http,
            GenerateMorningBriefingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var force = ParseForce(http.Request.Query);
            try
            {
                var result = await handler.HandleAsync(new GenerateMorningBriefingCommand(force), cancellationToken);
                return result.WasCached
                    ? Results.Ok(result.Briefing)
                    : Results.Created($"/api/briefings/{result.Briefing.Id}", result.Briefing);
            }
            catch (AiProviderNotConfiguredException ex)
            {
                return AiProviderNotConfiguredResult(ex);
            }
        }).RequireAuthorization();

        // POST /api/briefings/weekly?force=true|false
        app.MapPost("/api/briefings/weekly", async (
            HttpContext http,
            GenerateWeeklyBriefingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var force = ParseForce(http.Request.Query);
            try
            {
                var result = await handler.HandleAsync(new GenerateWeeklyBriefingCommand(force), cancellationToken);
                return result.WasCached
                    ? Results.Ok(result.Briefing)
                    : Results.Created($"/api/briefings/{result.Briefing.Id}", result.Briefing);
            }
            catch (AiProviderNotConfiguredException ex)
            {
                return AiProviderNotConfiguredResult(ex);
            }
        }).RequireAuthorization();

        // POST /api/briefings/one-on-one/{personId}?force=true|false
        app.MapPost("/api/briefings/one-on-one/{personId:guid}", async (
            Guid personId,
            HttpContext http,
            GenerateOneOnOnePrepHandler handler,
            CancellationToken cancellationToken) =>
        {
            var force = ParseForce(http.Request.Query);
            try
            {
                var result = await handler.HandleAsync(new GenerateOneOnOnePrepCommand(personId, force), cancellationToken);
                if (result is null) return Results.NotFound();

                return result.WasCached
                    ? Results.Ok(result.Briefing)
                    : Results.Created($"/api/briefings/{result.Briefing.Id}", result.Briefing);
            }
            catch (AiProviderNotConfiguredException ex)
            {
                return AiProviderNotConfiguredResult(ex);
            }
        }).RequireAuthorization();

        // GET /api/briefings/recent?type=...&limit=...
        app.MapGet("/api/briefings/recent", async (
            HttpContext http,
            GetRecentBriefingsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = http.Request.Query;

            BriefingTypeDto? type = null;
            if (query.TryGetValue("type", out var typeValues) && typeValues.Count > 0
                && !string.IsNullOrWhiteSpace(typeValues[0]))
            {
                if (!Enum.TryParse<BriefingTypeDto>(typeValues[0], ignoreCase: true, out var parsed))
                    return Results.BadRequest(new { error = $"Unknown briefing type '{typeValues[0]}'." });
                type = parsed;
            }

            var limit = GetRecentBriefingsHandler.DefaultLimit;
            if (query.TryGetValue("limit", out var limitValues) && limitValues.Count > 0
                && !string.IsNullOrWhiteSpace(limitValues[0]))
            {
                if (!int.TryParse(limitValues[0], out var parsed))
                    return Results.BadRequest(new { error = $"Invalid limit '{limitValues[0]}'." });
                limit = parsed;
            }

            if (limit < 1 || limit > GetRecentBriefingsHandler.MaxLimit)
                return Results.BadRequest(new { error = $"Limit must be between 1 and {GetRecentBriefingsHandler.MaxLimit}." });

            var result = await handler.HandleAsync(new GetRecentBriefingsQuery(type, limit), cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization();

        // GET /api/briefings/{id}
        app.MapGet("/api/briefings/{id:guid}", async (
            Guid id,
            GetBriefingHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new GetBriefingQuery(id), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization();

        return app;
    }

    private static bool ParseForce(IQueryCollection query)
    {
        if (!query.TryGetValue("force", out var values) || values.Count == 0) return false;
        var raw = values[0];
        return bool.TryParse(raw, out var force) && force;
    }

    private static IResult AiProviderNotConfiguredResult(AiProviderNotConfiguredException ex) =>
        Results.Json(
            new { error = ex.Message, code = "ai_provider_not_configured" },
            statusCode: StatusCodes.Status409Conflict);
}
