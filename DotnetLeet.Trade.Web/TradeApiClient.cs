using System.Net;
using System.Text.Json;
using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Application.Procurement.Ports;
using DotnetLeet.Trade.Application.Procurement.Queries;
using DotnetLeet.Trade.Application.Procurement.ReviewOffer;

namespace DotnetLeet.Trade.Web;

/// <summary>What the API answered when it could not do what was asked. The message is meant for the screen.</summary>
public sealed class TradeApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// The only way the Web project talks to the back office. One method per route,
/// the same record types the API serializes, default JSON options on both sides.
/// </summary>
public sealed class TradeApiClient(HttpClient http)
{
    // Offers

    public async Task<ExtractOfferFromEmailResult> ExtractOfferFromEmailAsync(string emailText, string? senderAddress, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync(
            "/procurement/offers/from-email",
            new ExtractOfferFromEmailCommand(emailText, senderAddress),
            ct);

        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<ExtractOfferFromEmailResult>(ct)
               ?? throw new TradeApiException("The API returned an empty response.", (int)response.StatusCode);
    }

    public async Task<IReadOnlyList<OfferSummary>> ListOffersAsync(int take = 50, CancellationToken ct = default) =>
        await GetAsync<List<OfferSummary>>($"/procurement/offers?take={take}", ct) ?? [];

    public async Task<OfferDetail?> GetOfferAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"/procurement/offers/{id}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<OfferDetail>(ct);
    }

    public async Task CorrectLineAsync(Guid offerId, Guid lineId, decimal? quantity, decimal? unitPrice, Guid? productId, CancellationToken ct = default)
    {
        using var response = await http.PutAsJsonAsync(
            $"/procurement/offers/{offerId}/lines/{lineId}",
            new CorrectOfferLineCommand(offerId, lineId, quantity, unitPrice, productId),
            ct);

        await EnsureSuccessAsync(response, ct);
    }

    public async Task CorrectHeaderAsync(Guid offerId, Guid? supplierId, string? currency, DateOnly? validUntil, int? paymentTermsDays, int? deliveryDays, CancellationToken ct = default)
    {
        using var response = await http.PutAsJsonAsync(
            $"/procurement/offers/{offerId}/header",
            new CorrectOfferHeaderCommand(offerId, supplierId, currency, validUntil, paymentTermsDays, deliveryDays),
            ct);

        await EnsureSuccessAsync(response, ct);
    }

    public async Task ConfirmReviewAsync(Guid offerId, CancellationToken ct = default)
    {
        using var response = await http.PostAsync($"/procurement/offers/{offerId}/confirm", content: null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task RejectAsync(Guid offerId, string reason, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync(
            $"/procurement/offers/{offerId}/reject",
            new RejectOfferCommand(offerId, reason),
            ct);

        await EnsureSuccessAsync(response, ct);
    }

    // Catalog

    public async Task<IReadOnlyList<ProductSummary>> SearchProductsAsync(string term, int take = 20, CancellationToken ct = default) =>
        await GetAsync<List<ProductSummary>>($"/catalog/products?q={Uri.EscapeDataString(term)}&take={take}", ct) ?? [];

    public async Task<IReadOnlyList<SupplierSummary>> ListSuppliersAsync(CancellationToken ct = default) =>
        await GetAsync<List<SupplierSummary>>("/catalog/suppliers", ct) ?? [];

    // AI

    public async Task<IReadOnlyList<AiCall>> ListAiCallsAsync(int take = 50, CancellationToken ct = default) =>
        await GetAsync<List<AiCall>>($"/ai/calls?take={take}", ct) ?? [];

    // Plumbing

    private async Task<T?> GetAsync<T>(string route, CancellationToken ct)
    {
        using var response = await http.GetAsync(route, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    /// <summary>Turns a failed response into one readable sentence. Problem details first, raw body second, status last.</summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(ct);
        var message = ProblemMessage(body) ?? $"The API answered {status} {response.ReasonPhrase}.";

        throw new TradeApiException(message, status);
    }

    private static string? ProblemMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return body;

            var root = document.RootElement;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(detail.GetString()))
                return detail.GetString();

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(title.GetString()))
                return title.GetString();

            return body;
        }
        catch (JsonException)
        {
            return body;
        }
    }
}
