using TitanOptimizer.Windows.Power;

namespace TitanOptimizer.Windows.Tests;

public sealed class PowerPlanChangeServiceTests
{
    [Fact]
    public void PreviewDoesNotMutateTheActivePlan()
    {
        var provider = new FakePowerPlanProvider();
        var service = new PowerPlanChangeService(provider);

        var plan = service.Preview(FakePowerPlanProvider.HighPerformanceGuid);
        var result = service.Execute(plan, TitanOptimizer.Core.Engine.OperationMode.Preview);

        Assert.True(result.Succeeded);
        Assert.True(result.WasDryRun);
        Assert.Equal(FakePowerPlanProvider.BalancedGuid, provider.GetActivePlan()!.Guid);
    }

    [Fact]
    public void ApplyThenRollbackRestoresTheOriginalPlan()
    {
        var provider = new FakePowerPlanProvider();
        var service = new PowerPlanChangeService(provider);
        var plan = service.Preview(FakePowerPlanProvider.HighPerformanceGuid);

        var applied = service.Execute(plan, TitanOptimizer.Core.Engine.OperationMode.Apply);
        var rolledBack = service.Rollback(plan);

        Assert.True(applied.Succeeded);
        Assert.True(rolledBack.Succeeded);
        Assert.Equal(FakePowerPlanProvider.BalancedGuid, provider.GetActivePlan()!.Guid);
    }

    [Fact]
    public void UnknownPlanIsRejectedBeforeMutation()
    {
        var provider = new FakePowerPlanProvider();
        var service = new PowerPlanChangeService(provider);

        Assert.Throws<InvalidOperationException>(() => service.Preview("00000000-0000-0000-0000-000000000000"));
        Assert.Equal(FakePowerPlanProvider.BalancedGuid, provider.GetActivePlan()!.Guid);
    }

    private sealed class FakePowerPlanProvider : IPowerPlanProvider
    {
        public const string BalancedGuid = "11111111-1111-1111-1111-111111111111";
        public const string HighPerformanceGuid = "22222222-2222-2222-2222-222222222222";

        private readonly PowerPlan[] _plans =
        [
            new(BalancedGuid, "Balanced"),
            new(HighPerformanceGuid, "High performance")
        ];

        private string _activeGuid = BalancedGuid;

        public IReadOnlyList<PowerPlan> ListPlans() => _plans;

        public PowerPlan? GetActivePlan() => _plans.First(plan => plan.Guid == _activeGuid);

        public void SetActivePlan(string guid)
        {
            if (_plans.All(plan => plan.Guid != guid))
            {
                throw new InvalidOperationException("Unknown test plan.");
            }

            _activeGuid = guid;
        }
    }
}
