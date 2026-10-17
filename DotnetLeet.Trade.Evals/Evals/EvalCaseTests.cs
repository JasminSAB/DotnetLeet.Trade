using DotnetLeet.Trade.Evals.Fakes;

namespace DotnetLeet.Trade.Evals.Evals;

/// <summary>The eval set itself is checked on every build: it loads, and it points at real catalog data.</summary>
public sealed class EvalCaseTests
{
    [Fact]
    public void Twelve_cases_load_and_reference_the_seed_catalog()
    {
        var cases = EvalCase.LoadAll();
        var seed = new InMemoryTrade(TradeHost.Today).Seed;

        Assert.Equal(12, cases.Count);

        foreach (var c in cases)
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Email), $"{c.Name}: empty email");
            Assert.NotEmpty(c.Expected.Lines);

            if (c.Expected.SupplierCode is not null)
                Assert.Contains(seed.Suppliers, s => s.Code == c.Expected.SupplierCode);

            foreach (var sku in c.Expected.Lines.Select(l => l.Sku).Where(s => s is not null))
                Assert.Contains(seed.Products, p => p.Sku == sku);

            Assert.Equal(c.Expected.Lines.Count(l => l.UnitPrice is null), c.Expected.UnpricedLines);
        }
    }
}
