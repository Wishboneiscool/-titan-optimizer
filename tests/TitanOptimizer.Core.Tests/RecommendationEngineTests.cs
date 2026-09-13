using TitanOptimizer.Core.Models;
using TitanOptimizer.Core.Recommendations;
using Xunit;

namespace TitanOptimizer.Core.Tests;

public sealed class RecommendationEngineTests
{
    [Fact]
    public void RecommendsReviewInsteadOfAutomaticPowerPlanSelection()
    {
        var definition = new OptimizationDefinition
        {
            Id = "power-plan.active",
            Name = "Select an installed power plan",
            Category = "Power",
            Subsystem = "Power plans",
            Description = "Review installed plans",
            Risk = RiskLevel.Low,
            Tier = QualityTier.ContextDependent,
            RequiresAdministrator = false,
            Reversible = true,
            BackupRequired = false,
            EvidenceSummary = "Context dependent"
        };

        var recommendations = new RecommendationEngine().Build(
            [definition],
            new RecommendationContext(true, 0, false, new HashSet<string>()));

        var recommendation = Assert.Single(recommendations);
        Assert.True(recommendation.RequiresUserReview);
        Assert.Equal("Context-dependent", recommendation.Confidence);
    }
}
