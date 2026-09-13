using TitanOptimizer.Core.Engine;
using TitanOptimizer.Core.Models;
using TitanOptimizer.Core.Safety;
using Xunit;

namespace TitanOptimizer.Core.Tests;

public sealed class OperationAuthorizationPolicyTests
{
    private static readonly OptimizationDefinition SafeDefinition = new()
    {
        Id = "test.safe",
        Name = "Safe test operation",
        Category = "Test",
        Subsystem = "Test",
        Description = "Test definition",
        Risk = RiskLevel.Low,
        Tier = QualityTier.StronglySupported,
        RequiresAdministrator = false,
        Reversible = true,
        BackupRequired = false,
        EvidenceSummary = "Test evidence"
    };

    [Fact]
    public void ApplyRequiresConfirmation()
    {
        var policy = new OperationAuthorizationPolicy();
        var result = policy.Validate(
            SafeDefinition,
            OperationMode.Apply,
            new AuthorizationContext(false, false, false));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void DryRunCanBePreviewedWithoutConfirmation()
    {
        var policy = new OperationAuthorizationPolicy();
        var result = policy.Validate(
            SafeDefinition,
            OperationMode.DryRun,
            new AuthorizationContext(false, false, true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectedDefinitionsAreAlwaysBlocked()
    {
        var policy = new OperationAuthorizationPolicy();
        var rejected = SafeDefinition with { Tier = QualityTier.Rejected };

        var result = policy.Validate(
            rejected,
            OperationMode.DryRun,
            new AuthorizationContext(false, false, true));

        Assert.False(result.IsValid);
    }
}
