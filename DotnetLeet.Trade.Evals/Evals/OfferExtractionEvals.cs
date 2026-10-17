using System.Text;
using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Application.Procurement.Instructions;
using DotnetLeet.Trade.Evals.Fakes;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Xunit.Abstractions;

namespace DotnetLeet.Trade.Evals.Evals;

/// <summary>
/// The real model against the twelve emails. Runs only with TRADE_EVALS=1, because it costs
/// time and tokens. One test per case, so the test runner is the eval report, and the fixture
/// writes evals-report.md with the per-case table and the field accuracy when the class is done.
///
/// TRADE_OLLAMA_ENDPOINT (default http://localhost:11434), TRADE_MODEL (default llama3.2:latest).
/// </summary>
public sealed class OfferExtractionEvals(ITestOutputHelper output, EvalReportFixture report) : IClassFixture<EvalReportFixture>
{
    public static TheoryData<string> Cases => [.. EvalCase.LoadAll().Select(c => c.Name)];

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

        var evalCase = EvalCase.LoadAll().Single(c => c.Name == name);
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

/// <summary>The development set report: evals-report.md and evals-raw/.</summary>
public sealed class EvalReportFixture : EvalReportFixtureBase
{
    public EvalReportFixture() : base("evals-report.md", "evals-raw") { }
}

/// <summary>
/// Collects one row per case and writes the report (plus the raw model answers) when every case
/// has run. A fixture, so no static state survives between runs.
/// </summary>
public abstract class EvalReportFixtureBase(string reportFile, string rawFolder) : IDisposable
{
    private readonly List<(EvalScore Score, string Model, long Tokens, TimeSpan Duration)> _rows = [];
    private readonly string _folder = AppContext.BaseDirectory;

    public void Add(EvalScore score, string model, long tokens, TimeSpan duration, string rawOutput)
    {
        lock (_rows)
            _rows.Add((score, model, tokens, duration));

        var raw = Path.Combine(_folder, rawFolder);
        Directory.CreateDirectory(raw);
        File.WriteAllText(Path.Combine(raw, score.Case + ".json"), rawOutput, Encoding.UTF8);
    }

    public void Dispose()
    {
        if (_rows.Count == 0)
            return;

        var passed = _rows.Sum(r => r.Score.Passed);
        var total = _rows.Sum(r => r.Score.Total);
        var model = _rows[0].Model;

        var sb = new StringBuilder();
        sb.AppendLine($"# Offer extraction evals, {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();
        sb.AppendLine($"Model {model}, instructions {OfferExtractionInstructions.Version}, {_rows.Count} cases, {passed}/{total} fields ({100.0 * passed / total:F0}%), {_rows.Count(r => r.Score.AllPassed)} cases fully correct, {_rows.Sum(r => r.Tokens)} tokens, {_rows.Sum(r => r.Duration.TotalSeconds):F0} s.");
        sb.AppendLine();
        sb.AppendLine("| Case | Fields | Tokens | Seconds | Failures |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var r in _rows.OrderBy(r => r.Score.Case))
        {
            var failures = string.Join("; ", r.Score.Failures.Select(f => $"{f.Field} expected '{f.Expected}' got '{f.Actual}'")).Replace("|", "\\|");
            sb.AppendLine($"| {r.Score.Case} | {r.Score.Passed}/{r.Score.Total} | {r.Tokens} | {r.Duration.TotalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} | {failures} |");
        }

        File.WriteAllText(Path.Combine(_folder, reportFile), sb.ToString(), Encoding.UTF8);
    }
}
