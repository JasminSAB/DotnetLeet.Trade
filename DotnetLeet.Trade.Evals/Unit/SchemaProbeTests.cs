using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Evals.Fakes;
using Microsoft.Extensions.AI;
using Xunit.Abstractions;

namespace DotnetLeet.Trade.Evals.Unit;

/// <summary>Prints what the adapter actually sends to the model: the response format and its schema.</summary>
public sealed class SchemaProbeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task The_adapter_asks_for_a_json_schema_response()
    {
        var model = new CannedChatClient("""{"lines":[],"unknowns":[]}""");
        var host = new TradeHost(model);

        await host.Extract.HandleAsync(new ExtractOfferFromEmailCommand("hello offer", "x@y.example"));

        var format = model.LastOptions?.ResponseFormat;
        output.WriteLine("ResponseFormat: " + (format?.GetType().Name ?? "null"));
        if (format is ChatResponseFormatJson json)
        {
            output.WriteLine("SchemaName: " + json.SchemaName);
            output.WriteLine("Schema: " + json.Schema);
        }
        output.WriteLine("Last user message: " + model.LastMessages[^1].Text);

        Assert.NotNull(format);
    }
}
