using System.Diagnostics;
using System.Text.Json;
using DotnetLeet.Trade.Application.Procurement.Instructions;
using DotnetLeet.Trade.Application.Procurement.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace DotnetLeet.Trade.Infrastructure.Ai;

public sealed class ExtractionOptions
{
    public const string Section = "Extraction";

    /// <summary>Zero. The same email should give the same draft.</summary>
    public float Temperature { get; set; } = 0f;

    /// <summary>Shown in the audit trail when the client does not report a model id.</summary>
    public string ModelName { get; set; } = "unknown";
}

/// <summary>
/// The adapter. It knows the chat client, the schema and the temperature.
/// It does not know what an offer is for; the instructions and the domain do.
/// </summary>
internal sealed class ModelOfferExtractor(IChatClient client, IOptions<ExtractionOptions> options, TimeProvider clock) : IOfferExtractor
{
    public const string Purpose = "offer-extraction";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The schema the model must fill. Every property is required, including the nullable ones.
    /// Without that, the grammar allows an empty object and a small model sometimes returns one.
    /// </summary>
    private static readonly ChatResponseFormat ResponseFormat = ChatResponseFormat.ForJsonSchema(
        AIJsonUtilities.CreateJsonSchema(
            typeof(OfferCompletion),
            description: "A supplier offer as written in the email. Unknown values are null, never guessed.",
            serializerOptions: Json,
            inferenceOptions: new AIJsonSchemaCreateOptions
            {
                TransformOptions = new AIJsonSchemaTransformOptions
                {
                    RequireAllProperties = true,
                    DisallowAdditionalProperties = true
                }
            }),
        schemaName: "SupplierOffer",
        schemaDescription: "A supplier offer as written in the email.");

    public async Task<OfferExtraction> ExtractAsync(string emailText, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, OfferExtractionInstructions.Text),
            new(ChatRole.User, emailText)
        };

        var chatOptions = new ChatOptions
        {
            Temperature = options.Value.Temperature,
            ResponseFormat = ResponseFormat
        };

        var watch = Stopwatch.StartNew();
        var response = await client.GetResponseAsync(messages, chatOptions, ct);
        watch.Stop();

        var call = new AiCall(
            Id: Guid.NewGuid(),
            Purpose: Purpose,
            InstructionsVersion: OfferExtractionInstructions.Version,
            Model: response.ModelId ?? options.Value.ModelName,
            Input: emailText,
            RawOutput: response.Text,
            InputTokens: response.Usage?.InputTokenCount ?? 0,
            OutputTokens: response.Usage?.OutputTokenCount ?? 0,
            Duration: watch.Elapsed,
            At: clock.GetUtcNow());

        OfferCompletion? completion;
        try
        {
            completion = JsonSerializer.Deserialize<OfferCompletion>(response.Text, Json);
        }
        catch (JsonException)
        {
            completion = null;
        }

        if (completion is null)
            throw new OfferExtractionFormatException(call);

        var (extraction, _) = completion.ToExtraction(call);
        return extraction;
    }
}

/// <summary>The model answered, but not in the shape we asked for. The call is kept for the audit trail.</summary>
public sealed class OfferExtractionFormatException(AiCall call)
    : Exception("The model did not return an offer in the expected shape.")
{
    public AiCall Call { get; } = call;
}
