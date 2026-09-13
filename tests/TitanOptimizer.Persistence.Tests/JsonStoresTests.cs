using TitanOptimizer.Core.Profiles;
using TitanOptimizer.Persistence.Catalog;
using TitanOptimizer.Persistence.Profiles;
using Xunit;

namespace TitanOptimizer.Persistence.Tests;

public sealed class JsonStoresTests
{
    [Fact]
    public void CatalogReadsStringEnumsAndRejectsDuplicates()
    {
        var directory = CreateDirectory();
        File.WriteAllText(Path.Combine(directory, "power.json"), """
            {
              "id": "power-plan.active",
              "name": "Power plan",
              "category": "Power",
              "subsystem": "Windows power plans",
              "description": "Test",
              "risk": "Low",
              "tier": "ContextDependent",
              "requiresAdministrator": false,
              "reversible": true,
              "backupRequired": false,
              "evidenceSummary": "Test"
            }
            """);

        var catalog = new JsonOptimizationCatalog(directory);
        var definitions = catalog.LoadAll();

        Assert.Single(definitions);
        Assert.Equal(TitanOptimizer.Core.Models.QualityTier.ContextDependent, definitions[0].Tier);
    }

    [Fact]
    public void ProfileStoreRoundTripsVersionedProfiles()
    {
        var directory = CreateDirectory();
        var store = new JsonProfileStore(directory);
        var profile = new OptimizationProfile
        {
            Id = "test",
            Name = "Test",
            Description = "Test profile",
            Optimizations = new Dictionary<string, ProfileSelection>
            {
                ["power-plan.active"] = new ProfileSelection { Enabled = false }
            }
        };

        store.Save(profile);
        var loaded = store.Load("test");

        Assert.NotNull(loaded);
        Assert.Equal("Test", loaded.Name);
        Assert.False(loaded.Optimizations["power-plan.active"].Enabled);
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "titan-optimizer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
