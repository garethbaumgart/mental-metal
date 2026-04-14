using MentalMetal.Application.Nudges;
using MentalMetal.Domain.Common;

namespace MentalMetal.Web.Features.Nudges;

public static class NudgesEndpoints
{
    public static IEndpointRouteBuilder MapNudgesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/nudges").RequireAuthorization();

        group.MapPost("/", async (
            CreateNudgeRequest request,
            CreateNudgeHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(request, ct);
                return Results.Created($"/api/nudges/{response.Id}", response);
            }
            catch (LinkedEntityNotFoundException ex) { return LinkedEntityNotFound(ex); }
            catch (DomainException ex) when (ex.Code == "nudge.invalidCadence") { return InvalidCadence(ex); }
            catch (ArgumentException ex) { return ValidationError(ex); }
        });

        group.MapGet("/", async (
            bool? isActive,
            Guid? personId,
            Guid? initiativeId,
            DateOnly? dueBefore,
            int? dueWithinDays,
            ListNudgesHandler handler,
            CancellationToken ct) =>
        {
            var filters = new ListNudgesFilters(isActive, personId, initiativeId, dueBefore, dueWithinDays);
            var list = await handler.HandleAsync(filters, ct);
            return Results.Ok(list);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetNudgeHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return NudgeNotFound(); }
        });

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateNudgeRequest request,
            UpdateNudgeHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return NudgeNotFound(); }
            catch (LinkedEntityNotFoundException ex) { return LinkedEntityNotFound(ex); }
            catch (ArgumentException ex) { return ValidationError(ex); }
        });

        group.MapPatch("/{id:guid}/cadence", async (
            Guid id,
            UpdateCadenceRequest request,
            UpdateNudgeCadenceHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return NudgeNotFound(); }
            catch (DomainException ex) when (ex.Code == "nudge.invalidCadence") { return InvalidCadence(ex); }
        });

        group.MapPost("/{id:guid}/mark-nudged", async (
            Guid id,
            MarkNudgeAsNudgedHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return NudgeNotFound(); }
            catch (DomainException ex) when (ex.Code == "nudge.notActive") { return Conflict(ex); }
        });

        group.MapPost("/{id:guid}/pause", async (
            Guid id,
            PauseNudgeHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return NudgeNotFound(); }
            catch (DomainException ex) when (ex.Code == "nudge.alreadyPaused") { return Conflict(ex); }
        });

        group.MapPost("/{id:guid}/resume", async (
            Guid id,
            ResumeNudgeHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return NudgeNotFound(); }
            catch (DomainException ex) when (ex.Code == "nudge.alreadyActive") { return Conflict(ex); }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteNudgeHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                await handler.HandleAsync(id, ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return NudgeNotFound(); }
        });

        return app;
    }

    private static IResult NudgeNotFound() =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Nudge not found.", extensions: new Dictionary<string, object?> { ["code"] = "nudge.notFound" });

    private static IResult ValidationError(Exception ex) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message, extensions: new Dictionary<string, object?> { ["code"] = "nudge.validation" });

    private static IResult InvalidCadence(Exception ex) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message, extensions: new Dictionary<string, object?> { ["code"] = "nudge.invalidCadence" });

    private static IResult LinkedEntityNotFound(Exception ex) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message, extensions: new Dictionary<string, object?> { ["code"] = "nudge.linkedEntityNotFound" });

    private static IResult Conflict(DomainException ex) =>
        Results.Problem(statusCode: StatusCodes.Status409Conflict, title: ex.Message, extensions: new Dictionary<string, object?> { ["code"] = ex.Code ?? "nudge.conflict" });
}
