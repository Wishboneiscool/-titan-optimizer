namespace TitanOptimizer.Windows.Power;

public sealed record PowerPlan(string Guid, string Name);

public interface IPowerPlanProvider
{
    IReadOnlyList<PowerPlan> ListPlans();
    PowerPlan? GetActivePlan();
    void SetActivePlan(string guid);
}

public sealed record PowerPlanChangePlan(PowerPlan Before, PowerPlan Target)
{
    public bool IsNoOp => string.Equals(Before.Guid, Target.Guid, StringComparison.OrdinalIgnoreCase);
}
