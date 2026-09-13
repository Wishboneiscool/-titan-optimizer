using TitanOptimizer.Core.Engine;

namespace TitanOptimizer.Windows.Power;

public sealed class PowerPlanChangeService
{
    private readonly IPowerPlanProvider _provider;

    public PowerPlanChangeService(IPowerPlanProvider provider)
    {
        _provider = provider;
    }

    public PowerPlanChangePlan Preview(string targetGuid)
    {
        var before = _provider.GetActivePlan()
            ?? throw new InvalidOperationException("The active power plan could not be detected.");

        var target = _provider.ListPlans()
            .FirstOrDefault(plan => string.Equals(plan.Guid, targetGuid, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The requested power plan is not present on this machine.");

        return new PowerPlanChangePlan(before, target);
    }

    public OperationResult Execute(PowerPlanChangePlan plan, OperationMode mode)
    {
        if (plan.IsNoOp)
        {
            return new OperationResult(true, mode != OperationMode.Apply, "The requested power plan is already active.", plan.Before.Guid, plan.Before.Guid);
        }

        if (mode is OperationMode.Preview)
        {
            return new OperationResult(true, true, $"Preview: switch from '{plan.Before.Name}' to '{plan.Target.Name}'.", plan.Before.Guid, plan.Target.Guid);
        }

        if (mode is OperationMode.DryRun)
        {
            return new OperationResult(true, true, $"Dry run: would switch from '{plan.Before.Name}' to '{plan.Target.Name}'.", plan.Before.Guid, plan.Target.Guid);
        }

        _provider.SetActivePlan(plan.Target.Guid);
        var actual = _provider.GetActivePlan();
        if (actual is null || !string.Equals(actual.Guid, plan.Target.Guid, StringComparison.OrdinalIgnoreCase))
        {
            TryRollback(plan.Before.Guid);
            throw new InvalidOperationException("The power-plan change could not be verified and was rolled back.");
        }

        return new OperationResult(true, false, "Power-plan change applied and verified.", plan.Before.Guid, actual.Guid);
    }

    public OperationResult Rollback(PowerPlanChangePlan plan, OperationMode mode = OperationMode.Apply)
    {
        if (mode is OperationMode.Preview)
        {
            return new OperationResult(true, true, $"Preview: restore '{plan.Before.Name}'.", plan.Target.Guid, plan.Before.Guid);
        }

        if (mode is OperationMode.DryRun)
        {
            return new OperationResult(true, true, $"Dry run: would restore '{plan.Before.Name}'.", plan.Target.Guid, plan.Before.Guid);
        }

        _provider.SetActivePlan(plan.Before.Guid);
        var actual = _provider.GetActivePlan();
        if (actual is null || !string.Equals(actual.Guid, plan.Before.Guid, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Power-plan rollback could not be verified.");
        }

        return new OperationResult(true, false, "Power-plan rollback applied and verified.", plan.Target.Guid, actual.Guid);
    }

    private void TryRollback(string guid)
    {
        try
        {
            _provider.SetActivePlan(guid);
        }
        catch
        {
            // Preserve the original verification failure. The audit layer records rollback failure separately.
        }
    }
}
