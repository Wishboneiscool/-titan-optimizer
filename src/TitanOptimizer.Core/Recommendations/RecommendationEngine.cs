using TitanOptimizer.Core.Models;

namespace TitanOptimizer.Core.Recommendations;

public sealed record RecommendationContext(
    bool HasMultiplePowerPlans,
    int StartupItemCount,
    bool IsOnBattery,
    IReadOnlySet<string> DetectedSubsystems);

public sealed record OptimizationRecommendation(
    string OptimizationId,
    string Title,
    string Rationale,
    string Confidence,
    bool RequiresUserReview);

public sealed class RecommendationEngine
{
    public IReadOnlyList<OptimizationRecommendation> Build(
        IEnumerable<OptimizationDefinition> definitions,
        RecommendationContext context)
    {
        var recommendations = new List<OptimizationRecommendation>();
        foreach (var definition in definitions)
        {
            if (definition.Tier == QualityTier.Rejected || definition.Risk is RiskLevel.High or RiskLevel.Critical)
            {
                continue;
            }

            if (definition.Id.Equals("power-plan.active", StringComparison.OrdinalIgnoreCase)
                && context.HasMultiplePowerPlans)
            {
                recommendations.Add(new OptimizationRecommendation(
                    definition.Id,
                    "Review installed power plans",
                    "Multiple plans are installed. Choose only if the workload, battery policy, and measured results justify the change.",
                    "Context-dependent",
                    true));
            }
        }

        return recommendations;
    }
}
