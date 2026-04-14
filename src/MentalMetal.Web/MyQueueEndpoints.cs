using MentalMetal.Application.MyQueue;
using MentalMetal.Application.MyQueue.Contracts;

namespace MentalMetal.Web;

public static class MyQueueEndpoints
{
    public static IEndpointRouteBuilder MapMyQueueEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/my-queue", async (
            HttpContext http,
            GetMyQueueHandler handler,
            CancellationToken cancellationToken) =>
        {
            var queryCol = http.Request.Query;

            // scope (enum, default All)
            var scope = QueueScope.All;
            if (queryCol.TryGetValue("scope", out var scopeValues) && scopeValues.Count > 0)
            {
                var raw = scopeValues[0];
                if (!Enum.TryParse<QueueScope>(raw, ignoreCase: true, out scope))
                {
                    return Results.BadRequest(new { error = $"Unknown scope '{raw}'." });
                }
            }

            // itemType (repeated, default all)
            var itemTypes = new List<QueueItemType>();
            if (queryCol.TryGetValue("itemType", out var typeValues))
            {
                foreach (var raw in typeValues)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    if (!Enum.TryParse<QueueItemType>(raw, ignoreCase: true, out var t))
                    {
                        return Results.BadRequest(new { error = $"Unknown itemType '{raw}'." });
                    }
                    itemTypes.Add(t);
                }
            }

            // personId / initiativeId (optional Guid)
            Guid? personId = null;
            if (queryCol.TryGetValue("personId", out var pidValues) && pidValues.Count > 0
                && !string.IsNullOrWhiteSpace(pidValues[0]))
            {
                if (!Guid.TryParse(pidValues[0], out var pid))
                    return Results.BadRequest(new { error = $"Invalid personId '{pidValues[0]}'." });
                personId = pid;
            }

            Guid? initiativeId = null;
            if (queryCol.TryGetValue("initiativeId", out var iidValues) && iidValues.Count > 0
                && !string.IsNullOrWhiteSpace(iidValues[0]))
            {
                if (!Guid.TryParse(iidValues[0], out var iid))
                    return Results.BadRequest(new { error = $"Invalid initiativeId '{iidValues[0]}'." });
                initiativeId = iid;
            }

            var query = new GetMyQueueQuery(scope, itemTypes, personId, initiativeId);
            var response = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        return app;
    }
}
