using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Application.Procurement.Queries;
using DotnetLeet.Trade.Application.Procurement.ReviewOffer;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DotnetLeet.Trade.ApiService;

public static class ProcurementEndpoints
{
    public static IEndpointRouteBuilder MapProcurementEndpoints(this IEndpointRouteBuilder app)
    {
        var offers = app.MapGroup("/procurement/offers").WithTags("Offers");

        offers.MapPost("/from-email", async Task<Results<Created<ExtractOfferFromEmailResult>, Ok<ExtractOfferFromEmailResult>>> (
            ExtractOfferFromEmailCommand command, ExtractOfferFromEmailHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(command, ct);
            return result.AlreadyExisted
                ? TypedResults.Ok(result)
                : TypedResults.Created($"/procurement/offers/{result.OfferId}", result);
        }).WithName("ExtractOfferFromEmail");

        offers.MapGet("/", (OfferQueries queries, CancellationToken ct, int take = 50) => queries.ListAsync(take, ct))
            .WithName("ListOffers");

        offers.MapGet("/{id:guid}", async Task<Results<Ok<OfferDetail>, NotFound>> (Guid id, OfferQueries queries, CancellationToken ct) =>
        {
            var offer = await queries.GetAsync(id, ct);
            return offer is null ? TypedResults.NotFound() : TypedResults.Ok(offer);
        }).WithName("GetOffer");

        offers.MapPut("/{id:guid}/lines/{lineId:guid}", async (Guid id, Guid lineId, CorrectOfferLineCommand command, ReviewOfferHandler handler, CancellationToken ct) =>
        {
            await handler.CorrectLineAsync(command with { OfferId = id, LineId = lineId }, ct);
            return Results.NoContent();
        }).WithName("CorrectOfferLine");

        offers.MapPut("/{id:guid}/header", async (Guid id, CorrectOfferHeaderCommand command, ReviewOfferHandler handler, CancellationToken ct) =>
        {
            await handler.CorrectHeaderAsync(command with { OfferId = id }, ct);
            return Results.NoContent();
        }).WithName("CorrectOfferHeader");

        offers.MapPost("/{id:guid}/confirm", async (Guid id, ReviewOfferHandler handler, CancellationToken ct) =>
        {
            try
            {
                await handler.ConfirmAsync(new ConfirmOfferReviewCommand(id), ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // the domain said no: blocking issues, no lines, or the offer is no longer a draft
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict, title: "The offer cannot be confirmed");
            }
        }).WithName("ConfirmOfferReview");

        offers.MapPost("/{id:guid}/reject", async (Guid id, RejectOfferCommand command, ReviewOfferHandler handler, CancellationToken ct) =>
        {
            await handler.RejectAsync(command with { OfferId = id }, ct);
            return Results.NoContent();
        }).WithName("RejectOffer");

        var catalog = app.MapGroup("/catalog").WithTags("Catalog");

        catalog.MapGet("/products", (string q, OfferQueries queries, CancellationToken ct, int take = 20) => queries.SearchProductsAsync(q, take, ct))
            .WithName("SearchProducts");

        catalog.MapGet("/suppliers", (OfferQueries queries, CancellationToken ct) => queries.ListSuppliersAsync(ct))
            .WithName("ListSuppliers");

        var ai = app.MapGroup("/ai").WithTags("AI");

        ai.MapGet("/calls", (OfferQueries queries, CancellationToken ct, int take = 50) => queries.ListAiCallsAsync(take, ct))
            .WithName("ListAiCalls");

        return app;
    }
}
