using MentalMetal.Application.Briefings;
using MentalMetal.Application.Interviews;
using MentalMetal.Domain.Common;
using MentalMetal.Domain.Interviews;

namespace MentalMetal.Web.Features.Interviews;

public static class InterviewEndpoints
{
    public static IEndpointRouteBuilder MapInterviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/interviews").RequireAuthorization();

        group.MapPost("/", async (
            CreateInterviewRequest request,
            CreateInterviewHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(request, ct);
                return Results.Created($"/api/interviews/{response.Id}", response);
            }
            catch (CandidateNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message, code = "candidate_not_found" });
            }
            catch (CandidateWrongTypeException ex)
            {
                return Results.BadRequest(new { error = ex.Message, code = "candidate_wrong_type" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message, code = "validation_failed" });
            }
        });

        group.MapGet("/", async (
            Guid? candidatePersonId,
            InterviewStage? stage,
            GetUserInterviewsHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.HandleAsync(candidatePersonId, stage, ct);
            return Results.Ok(list);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetInterviewByIdHandler handler,
            CancellationToken ct) =>
        {
            var response = await handler.HandleAsync(id, ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateInterviewRequest request,
            UpdateInterviewHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/advance", async (
            Guid id,
            AdvanceInterviewStageRequest request,
            AdvanceInterviewStageHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (DomainException ex) { return DomainConflict(ex); }
        });

        group.MapPost("/{id:guid}/decision", async (
            Guid id,
            RecordInterviewDecisionRequest request,
            RecordInterviewDecisionHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (DomainException ex) { return DomainConflict(ex); }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteInterviewHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                await handler.HandleAsync(id, ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/{id:guid}/scorecards", async (
            Guid id,
            UpsertScorecardRequest request,
            AddInterviewScorecardHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, ct);
                return Results.Created($"/api/interviews/{id}/scorecards/{response.Id}", response);
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message, code = "validation_failed" }); }
        });

        group.MapPut("/{id:guid}/scorecards/{scorecardId:guid}", async (
            Guid id,
            Guid scorecardId,
            UpsertScorecardRequest request,
            UpdateInterviewScorecardHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, scorecardId, request, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ScorecardNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message, code = "scorecard_not_found" });
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message, code = "validation_failed" }); }
        });

        group.MapDelete("/{id:guid}/scorecards/{scorecardId:guid}", async (
            Guid id,
            Guid scorecardId,
            RemoveInterviewScorecardHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                await handler.HandleAsync(id, scorecardId, ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ScorecardNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message, code = "scorecard_not_found" });
            }
        });

        group.MapPut("/{id:guid}/transcript", async (
            Guid id,
            SetTranscriptRequest request,
            SetInterviewTranscriptHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, request, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (TranscriptTooLongException ex)
            {
                return Results.Json(
                    new { error = ex.Message, code = "transcript_too_long" },
                    statusCode: StatusCodes.Status413PayloadTooLarge);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:guid}/analyze", async (
            Guid id,
            AnalyzeInterviewHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var response = await handler.HandleAsync(id, ct);
                return Results.Ok(response);
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (DomainException ex) { return DomainConflict(ex); }
            catch (AiProviderNotConfiguredException ex)
            {
                return Results.Json(
                    new { error = ex.Message, code = "ai_provider_not_configured" },
                    statusCode: StatusCodes.Status409Conflict);
            }
            catch (InterviewAnalysisFailedException ex)
            {
                return Results.Json(
                    new { error = ex.Message, code = "analysis_failed" },
                    statusCode: StatusCodes.Status502BadGateway);
            }
        });

        return app;
    }

    private static IResult DomainConflict(DomainException ex) =>
        Results.Json(
            new { error = ex.Message, code = ex.Code ?? "domain_conflict" },
            statusCode: StatusCodes.Status409Conflict);
}
