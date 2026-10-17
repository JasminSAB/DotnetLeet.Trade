namespace DotnetLeet.Trade.Domain.Catalog;

public sealed class Supplier
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    /// <summary>ISO 4217, the currency this supplier quotes in.</summary>
    public string Currency { get; private set; } = "EUR";
    public int DefaultPaymentTermsDays { get; private set; }
    /// <summary>The domain the supplier's offers come from, used to recognise the sender.</summary>
    public string EmailDomain { get; private set; } = "";
    public bool Active { get; private set; } = true;

    private Supplier() { }

    public static Supplier Create(string code, string name, string currency, int paymentTermsDays, string emailDomain)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("A supplier needs a code.", nameof(code));
        if (currency.Length != 3) throw new ArgumentException("Currency is an ISO 4217 code.", nameof(currency));

        return new Supplier
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Currency = currency.ToUpperInvariant(),
            DefaultPaymentTermsDays = paymentTermsDays,
            EmailDomain = emailDomain.Trim().ToLowerInvariant()
        };
    }
}
