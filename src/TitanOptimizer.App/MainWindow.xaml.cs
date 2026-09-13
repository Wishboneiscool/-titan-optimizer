using System.Windows;
using TitanOptimizer.Core.Benchmarking;
using TitanOptimizer.Core.Engine;
using TitanOptimizer.Core.Models;
using TitanOptimizer.Persistence;
using TitanOptimizer.Windows.Power;
using TitanOptimizer.Windows.System;

namespace TitanOptimizer.App;

public partial class MainWindow : Window
{
    private readonly IPowerPlanProvider _powerPlanProvider;
    private readonly PowerPlanChangeService _powerPlanService;
    private readonly WindowsSystemProfiler _systemProfiler;
    private readonly SqliteChangeJournal _journal;
    private PowerPlanChangePlan? _lastPlan;

    public MainWindow()
    {
        InitializeComponent();
        _powerPlanProvider = new PowerCfgPowerPlanProvider();
        _powerPlanService = new PowerPlanChangeService(_powerPlanProvider);
        _systemProfiler = new WindowsSystemProfiler();

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TitanOptimizer");
        _journal = new SqliteChangeJournal(Path.Combine(dataDirectory, "titan-optimizer.db"));
        RefreshHistory();
    }

    private void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var snapshot = _systemProfiler.Capture();
            var plans = _powerPlanProvider.ListPlans();
            var active = _powerPlanProvider.GetActivePlan();
            PowerPlanComboBox.ItemsSource = plans;
            PowerPlanComboBox.SelectedItem = plans.FirstOrDefault(plan => plan.Guid == active?.Guid);
            ActivePlanText.Text = active is null ? "Unable to detect" : $"{active.Name} ({active.Guid})";
            SetStatus($"Scan complete: {snapshot.CpuLogicalProcessors} logical processors, {FormatBytes(snapshot.AvailableMemoryBytes)} available memory, {plans.Count} power plans.", false);
            PlanDetailsText.Text = FormatSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            SetStatus($"Scan skipped: {ex.Message}", true);
        }
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (PowerPlanComboBox.SelectedItem is not PowerPlan target)
        {
            SetStatus("Choose a target power plan first.", true);
            return;
        }

        try
        {
            _lastPlan = _powerPlanService.Preview(target.Guid);
            PlanDetailsText.Text = _lastPlan.IsNoOp
                ? "The selected plan is already active. No change is needed."
                : $"Preview: {_lastPlan.Before.Name} → {_lastPlan.Target.Name}. The original plan will be available for rollback.";
            SetStatus("Preview complete. No system change was made.", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Preview skipped: {ex.Message}", true);
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (PowerPlanComboBox.SelectedItem is not PowerPlan target)
        {
            SetStatus("Choose a target power plan first.", true);
            return;
        }

        try
        {
            _lastPlan = _powerPlanService.Preview(target.Guid);
            if (_lastPlan.IsNoOp)
            {
                SetStatus("The selected plan is already active.", false);
                return;
            }

            var confirmation = MessageBox.Show(
                $"Switch from '{_lastPlan.Before.Name}' to '{_lastPlan.Target.Name}'?\n\nThe change will be verified and can be rolled back.",
                "Confirm power-plan change",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
            {
                SetStatus("Change cancelled. No system change was made.", false);
                return;
            }

            var result = _powerPlanService.Execute(_lastPlan, OperationMode.Apply);
            WriteRecord(_lastPlan, result, "power-plan.active");
            SetStatus(result.Message, false);
            ActivePlanText.Text = $"{_lastPlan.Target.Name} ({_lastPlan.Target.Guid})";
            PlanDetailsText.Text = "Applied and verified. Use Rollback to restore the original plan.";
        }
        catch (Exception ex)
        {
            SetStatus($"Change failed safely: {ex.Message}", true);
        }
    }

    private void RollbackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastPlan is null)
        {
            SetStatus("There is no applied change in this session to roll back.", true);
            return;
        }

        var confirmation = MessageBox.Show(
            $"Restore '{_lastPlan.Before.Name}'?",
            "Confirm rollback",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
        {
            SetStatus("Rollback cancelled.", false);
            return;
        }

        try
        {
            var result = _powerPlanService.Rollback(_lastPlan);
            WriteRecord(_lastPlan, result, "power-plan.active.rollback");
            SetStatus(result.Message, false);
            ActivePlanText.Text = $"{_lastPlan.Before.Name} ({_lastPlan.Before.Guid})";
            PlanDetailsText.Text = "Rollback applied and verified.";
        }
        catch (Exception ex)
        {
            SetStatus($"Rollback failed: {ex.Message}", true);
        }
    }

    private async void BenchmarkButton_Click(object sender, RoutedEventArgs e)
    {
        BenchmarkButton.IsEnabled = false;
        BenchmarkText.Text = "Running a deterministic local workload…";
        try
        {
            var sample = await Task.Run(() => new SyntheticCpuBenchmark().Run());
            BenchmarkText.Text = $"{sample.WorkUnitsPerSecond:N0} work units/sec ({sample.Duration.TotalMilliseconds:N0} ms).";
        }
        catch (Exception ex)
        {
            BenchmarkText.Text = $"Benchmark unavailable: {ex.Message}";
        }
        finally
        {
            BenchmarkButton.IsEnabled = true;
        }
    }

    private void WriteRecord(PowerPlanChangePlan plan, OperationResult result, string optimizationId)
    {
        _journal.Append(new ChangeRecord
        {
            SessionId = Guid.NewGuid(),
            OptimizationId = optimizationId,
            TimestampUtc = DateTimeOffset.UtcNow,
            Before = plan.Before.Guid,
            Requested = plan.Target.Guid,
            After = result.After,
            Result = result.Succeeded ? "Verified" : "Failed",
            RollbackAvailable = true,
            Error = null
        });
        RefreshHistory();
    }

    private void RefreshHistory()
    {
        var records = _journal.GetRecent(20);
        HistoryList.ItemsSource = records.Count == 0
            ? new[] { "No applied changes recorded yet." }
            : records.Select(record =>
                $"{record.TimestampUtc.ToLocalTime():g}  •  {record.OptimizationId}  •  {record.Result}").ToArray();
    }

    private static string FormatSnapshot(TitanOptimizer.Core.Diagnostics.SystemSnapshot snapshot)
    {
        var volumes = snapshot.Volumes.Count == 0
            ? "no readable volumes"
            : string.Join(", ", snapshot.Volumes.Select(volume => $"{volume.Volume} {FormatBytes(volume.AvailableBytes)} free"));
        return $"{snapshot.OperatingSystem}. Total memory: {FormatBytes(snapshot.TotalMemoryBytes)}. Storage: {volumes}.";
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var suffix = 0;
        while (value >= 1024 && suffix < suffixes.Length - 1)
        {
            value /= 1024;
            suffix++;
        }

        return $"{value:0.##} {suffixes[suffix]}";
    }

    private static string FormatBytes(long bytes) => FormatBytes((ulong)Math.Max(0, bytes));

    private void SetStatus(string message, bool isWarning)
    {
        StatusText.Text = message;
        StatusText.Foreground = isWarning
            ? (System.Windows.Media.Brush)FindResource("Warning")
            : (System.Windows.Media.Brush)FindResource("SecondaryText");
    }
}
