using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TitanOptimizer.Windows.Power;

/// <summary>
/// Narrow adapter for the built-in Windows powercfg executable.
/// No caller-provided executable or free-form command text is accepted.
/// </summary>
public sealed class PowerCfgPowerPlanProvider : IPowerPlanProvider
{
    private static readonly Regex PlanRegex = new(
        @"(?<guid>[0-9a-fA-F-]{36})\s+\((?<name>[^\r\n]*)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IReadOnlyList<PowerPlan> ListPlans()
    {
        EnsureWindows();
        var output = Run("/list");
        return PlanRegex.Matches(output)
            .Select(match => new PowerPlan(match.Groups["guid"].Value, match.Groups["name"].Value.Trim()))
            .DistinctBy(plan => plan.Guid, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public PowerPlan? GetActivePlan()
    {
        EnsureWindows();
        var output = Run("/getactivescheme");
        var match = PlanRegex.Match(output);
        return match.Success
            ? new PowerPlan(match.Groups["guid"].Value, match.Groups["name"].Value.Trim())
            : null;
    }

    public void SetActivePlan(string guid)
    {
        EnsureWindows();
        if (!Guid.TryParse(guid, out _))
        {
            throw new ArgumentException("The power-plan identifier is not a valid GUID.", nameof(guid));
        }

        _ = Run($"/setactive {guid}");
    }

    private static string Run(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"powercfg.exe failed with exit code {process.ExitCode}: {error.Trim()}");
        }

        return output;
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Power-plan inspection is supported only on Windows.");
        }
    }
}
