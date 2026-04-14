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
            bool force;
            try { force = TryParseForce(http.Request.Query) ?? false; }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
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
            bool force;
            try { force = TryParseForce(http.Request.Query) ?? false; }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
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
            bool force;
            try { force = TryParseForce(http.Request.Query) ?? false; }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
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
                // Enum.TryParse accepts numeric strings even when they don't map to a
                // defined enum value (e.g. type=999) - guard with Enum.IsDefined so
                // bad input is rejected with a 400 instead of silently sneaking through.
                if (!Enum.TryParse<BriefingTypeDto>(typeValues[0], ignoreCase: true, out var parsed)
                    || !Enum.IsDefined(parsed))
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

    /// <summary>
    /// Parses the optional `force` query parameter. Returns null when the parameter
    /// is absent (caller treats null as false). Returns a boolean for "true"/"false"
    /// (case-insensitive). Throws <see cref="ArgumentException"/> for any other value
    /// so the endpoint can map it to HTTP 400.
    /// </summary>
    private static bool? TryParseForce(IQueryCollection query)
    {
        if (!query.TryGetValue("force", out var values) || values.Count == 0) return null;
        var raw = values[0];
        if (string.IsNullOrEmpty(raw)) return null;
        if (!bool.TryParse(raw, out var force))
            throw new ArgumentException($"Invalid 'force' value '{raw}'. Expected true|false.");
        return force;
    }

    private static IResult AiProviderNotConfiguredResult(AiProviderNotConfiguredException ex) =>
        Results.Json(
            new { error = ex.Message, code = "ai_provider_not_configured" },
            statusCode: StatusCodes.Status409Conflict);
}
