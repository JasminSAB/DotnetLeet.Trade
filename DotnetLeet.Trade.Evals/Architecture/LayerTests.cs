using System.Xml.Linq;
using DotnetLeet.Trade.Application.Procurement.ExtractOffer;
using DotnetLeet.Trade.Domain.Procurement;

namespace DotnetLeet.Trade.Evals.Architecture;

/// <summary>
/// The dependency rules as tests. They fail the build, not a code review.
/// </summary>
public sealed class LayerTests
{
    private static readonly string[] AiPackages = ["Microsoft.Extensions.AI", "OllamaSharp", "OpenAI", "CommunityToolkit.Aspire"];
    private static readonly string[] PersistencePackages = ["Microsoft.EntityFrameworkCore", "Npgsql"];

    [Fact]
    public void Domain_references_no_ai_no_database_and_no_other_layer()
    {
        var references = ReferencesOf(typeof(SupplierOffer).Assembly);

        Assert.DoesNotContain(references, r => AiPackages.Any(p => r.StartsWith(p, StringComparison.Ordinal)));
        Assert.DoesNotContain(references, r => PersistencePackages.Any(p => r.StartsWith(p, StringComparison.Ordinal)));
        Assert.DoesNotContain(references, r => r.StartsWith("DotnetLeet.Trade.", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_references_only_the_domain()
    {
        var references = ReferencesOf(typeof(ExtractOfferFromEmailHandler).Assembly);

        Assert.DoesNotContain(references, r => AiPackages.Any(p => r.StartsWith(p, StringComparison.Ordinal)));
        Assert.DoesNotContain(references, r => PersistencePackages.Any(p => r.StartsWith(p, StringComparison.Ordinal)));
        Assert.DoesNotContain("DotnetLeet.Trade.Infrastructure", references);
        Assert.Contains("DotnetLeet.Trade.Domain", references);
    }

    [Fact]
    public void Infrastructure_is_the_only_layer_that_talks_to_the_model_and_the_database()
    {
        var references = ReferencesOf(typeof(Infrastructure.DependencyInjection).Assembly);

        Assert.Contains(references, r => r.StartsWith("Microsoft.Extensions.AI", StringComparison.Ordinal));
        Assert.Contains(references, r => r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void The_web_frontend_does_not_reference_infrastructure()
    {
        var csproj = FindProjectFile("DotnetLeet.Trade.Web");
        var references = XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(r => r.Attribute("Include")!.Value)
            .ToList();

        Assert.DoesNotContain(references, r => r.Contains("DotnetLeet.Trade.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => r.Contains("DotnetLeet.Trade.Domain", StringComparison.Ordinal));
    }

    private static List<string> ReferencesOf(System.Reflection.Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

    private static string FindProjectFile(string projectName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, projectName, $"{projectName}.csproj");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"{projectName}.csproj not found above {AppContext.BaseDirectory}");
    }
}
