using TitanOptimizer.Core.Engine;
using TitanOptimizer.Core.Models;

namespace TitanOptimizer.Core.Safety;

public sealed record AuthorizationContext(
    bool IsAdministrator,
    bool UserConfirmed,
    bool IsDryRun);

public sealed class OperationAuthorizationPolicy
{
    public ValidationResult Validate(
        OptimizationDefinition definition,
        OperationMode mode,
        AuthorizationContext context)
    {
        if (definition.Tier == QualityTier.Rejected)
        {
            return ValidationResult.Invalid("Rejected optimizations cannot be applied.");
        }

        if (mode == OperationMode.Apply && !definition.Reversible)
        {
            return ValidationResult.Invalid("Non-reversible optimizations require a separate explicit workflow.");
        }

        if (definition.RequiresAdministrator && !context.IsAdministrator && !context.IsDryRun)
        {
            return ValidationResult.Invalid("Administrator privileges are required for this operation.");
        }

        if (mode == OperationMode.Apply && !context.UserConfirmed)
        {
            return ValidationResult.Invalid("Explicit user confirmation is required before applying a change.");
        }

        if (definition.Risk is RiskLevel.High or RiskLevel.Critical
            && mode == OperationMode.Apply
            && !context.UserConfirmed)
        {
            return ValidationResult.Invalid("High-risk operations require explicit confirmation.");
        }

        return ValidationResult.Valid();
    }
}
