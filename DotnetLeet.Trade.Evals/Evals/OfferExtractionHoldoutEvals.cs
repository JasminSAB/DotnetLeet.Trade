using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Evals.Fakes;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Xunit.Abstractions;

namespace DotnetLeet.Trade.Evals.Evals;

/// <summary>
/// The held-out emails: written after the instructions, the rules and the checker were frozen,
/// and never used to change anything. If the system changes after this run, these cases join
/// the development set and a new held-out set is needed. Same runner, same checks, separate report.
/// </summary>
public sealed class OfferExtractionHoldoutEvals(ITestOutputHelper output, HoldoutReportFixture report) : IClassFixture<HoldoutReportFixture>
{
    public static TheoryData<string> Cases => [.. EvalCase.LoadHoldout().Select(c => c.Name)];

    private static readonly Lazy<IChatClient> Client = new(() =>
    {
        var endpoint = Environment.GetEnvironmentVariable("TRADE_OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        var model = Environment.GetEnvironmentVariable("TRADE_MODEL") ?? "llama3.2:latest";
        return new OllamaApiClient(new Uri(endpoint), model);
    });

    [SkippableTheory]
    [MemberData(nameof(Cases))]
    public async Task Case(string name)
    {
        Skip.IfNot(Environment.GetEnvironmentVariable("TRADE_EVALS") == "1", "set TRADE_EVALS=1 to run the model evals");

        var evalCase = EvalCase.LoadHoldout().Single(c => c.Name == name);
        var host = new TradeHost(Client.Value);

        var result = await host.Extract.HandleAsync(new ExtractOfferFromEmailCommand(evalCase.Email, evalCase.SenderAddress));
        var offer = (await host.Queries.GetAsync(result.OfferId))!;
        var supplierCode = offer.SupplierId is { } sid ? host.Store.Seed.Suppliers.Single(s => s.Id == sid).Code : null;
        var call = host.Store.AiCalls.Single();

        var score = evalCase.Score(offer, supplierCode);

        output.WriteLine($"{name}: {score.Passed}/{score.Total} fields, {call.InputTokens}+{call.OutputTokens} tokens, {call.Duration.TotalSeconds:F1}s, model {call.Model}");
        foreach (var f in score.Failures)
            output.WriteLine($"  FAIL {f.Field}: expected '{f.Expected}', got '{f.Actual}'");

        report.Add(score, call.Model, call.InputTokens + call.OutputTokens, call.Duration, call.RawOutput);

        Assert.True(score.AllPassed, $"{score.Failures.Count()} of {score.Total} field checks failed. See the test output.");
    }
}

/// <summary>Same report as the development set, written to evals-holdout-report.md and evals-holdout-raw/.</summary>
public sealed class HoldoutReportFixture : EvalReportFixtureBase
{
    public HoldoutReportFixture() : base("evals-holdout-report.md", "evals-holdout-raw") { }
}
