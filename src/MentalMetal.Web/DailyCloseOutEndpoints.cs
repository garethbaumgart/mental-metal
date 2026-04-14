using MentalMetal.Application.DailyCloseOut;

namespace MentalMetal.Web;

public static class DailyCloseOutEndpoints
{
    public static IEndpointRouteBuilder MapDailyCloseOutEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/daily-close-out/queue", async (
            GetCloseOutQueueHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapPost("/api/daily-close-out/captures/{id:guid}/quick-discard", async (
            Guid id,
            QuickDiscardCaptureHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await handler.HandleAsync(id, cancellationToken);
                return Results.Ok();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound();
            }
        }).RequireAuthorization();

        app.MapPost("/api/daily-close-out/captures/{id:guid}/reassign", async (
            Guid id,
            ReassignCaptureRequest request,
            ReassignCaptureHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, cancellationToken);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        app.MapPost("/api/daily-close-out/close", async (
            CloseOutDayRequest? request,
            CloseOutDayHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await handler.HandleAsync(
                    request ?? new CloseOutDayRequest(null), cancellationToken);
                return Results.Ok(response);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        app.MapGet("/api/daily-close-out/log", async (
            int? limit,
            GetCloseOutLogHandler handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(limit, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        return app;
    }
}
