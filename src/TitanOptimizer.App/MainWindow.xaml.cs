using System.IO;
using System.Windows;
using System.Windows.Controls;
using TitanOptimizer.Core.Benchmarking;
using TitanOptimizer.Core.Configuration;
using TitanOptimizer.Core.Engine;
using TitanOptimizer.Core.Models;
using TitanOptimizer.Core.Profiles;
using TitanOptimizer.Core.Recommendations;
using TitanOptimizer.Core.Safety;
using TitanOptimizer.Persistence;
using TitanOptimizer.Persistence.Catalog;
using TitanOptimizer.Persistence.Configuration;
using TitanOptimizer.Persistence.Profiles;
using TitanOptimizer.Windows.Network;
using TitanOptimizer.Windows.Power;
using TitanOptimizer.Windows.Security;
using TitanOptimizer.Windows.Startup;
using TitanOptimizer.Windows.System;

namespace TitanOptimizer.App;

public partial class MainWindow : Window
{
    private readonly IPowerPlanProvider _powerPlanProvider;
    private readonly PowerPlanChangeService _powerPlanService;
    private readonly WindowsSystemProfiler _systemProfiler;
    private readonly WindowsSecurityContext _securityContext;
    private readonly WindowsStartupInventory _startupInventory;
    private readonly WindowsNetworkDiagnostics _networkDiagnostics;
    private readonly SqliteChangeJournal _journal;
    private readonly SqliteBenchmarkJournal _benchmarkJournal;
    private readonly JsonOptimizationCatalog _catalog;
    private readonly JsonProfileStore _profileStore;
    private readonly JsonSettingsStore _settingsStore;
    private readonly OperationAuthorizationPolicy _authorizationPolicy;
    private readonly RecommendationEngine _recommendationEngine;
    private AppSettings _settings;
    private PowerPlanChangePlan? _lastPlan;

    public MainWindow()
    {
        InitializeComponent();
        _powerPlanProvider = new PowerCfgPowerPlanProvider();
        _powerPlanService = new PowerPlanChangeService(_powerPlanProvider);
        _systemProfiler = new WindowsSystemProfiler();
        _securityContext = new WindowsSecurityContext();
        _startupInventory = new WindowsStartupInventory();
        _networkDiagnostics = new WindowsNetworkDiagnostics();
        _catalog = new JsonOptimizationCatalog(Path.Combine(AppContext.BaseDirectory, "data", "optimizations"));
        _profileStore = new JsonProfileStore(Path.Combine(AppContext.BaseDirectory, "data", "profiles"));
        _authorizationPolicy = new OperationAuthorizationPolicy();
        _recommendationEngine = new RecommendationEngine();

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TitanOptimizer");
        _settingsStore = new JsonSettingsStore(Path.Combine(dataDirectory, "settings.json"));
        _settings = _settingsStore.Load();
        AutomaticRecommendationsCheckBox.IsChecked = _settings.AutomaticRecommendations;
        ShowExperimentalCheckBox.IsChecked = _settings.ShowExperimentalDefinitions;
        ReducedMotionCheckBox.IsChecked = _settings.ReducedMotion;

        var profiles = _profileStore.List();
        ProfileComboBox.ItemsSource = profiles;
        ProfileComboBox.SelectedIndex = Math.Max(
            0,
            profiles.Select((profile, index) => new { profile, index })
                .FirstOrDefault(item => item.profile.Id.Equals(_settings.ActiveProfileId, StringComparison.OrdinalIgnoreCase))?.index ?? 0);

        var databasePath = Path.Combine(dataDirectory, "titan-optimizer.db");
        _journal = new SqliteChangeJournal(databasePath);
        _benchmarkJournal = new SqliteBenchmarkJournal(databasePath);
        RestoreLastPlanFromHistory();
        RefreshHistory();
    }

    private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileComboBox.SelectedItem is not OptimizationProfile profile)
        {
            return;
        }

        _settings = _settings with { ActiveProfileId = profile.Id };
        _settingsStore.Save(_settings);
        ProfileDescriptionText.Text = profile.Description;
        SetStatus($"Profile selected: {profile.Name}. No system changes were applied.", false);
    }

    private void WorkspaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        switch (button.Tag as string)
        {
            case "dashboard":
                OverviewSection.BringIntoView();
                SetStatus("Dashboard overview selected.", false);
                break;
            case "recommendations":
                RecommendationsSection.BringIntoView();
                SetStatus("Recommendations selected. Review items are never applied automatically.", false);
                break;
            case "history":
                HistorySection.BringIntoView();
                SetStatus("Change history selected.", false);
                break;
            case "diagnostics":
                DiagnosticsSection.BringIntoView();
                SetStatus("Read-only diagnostics selected.", false);
                break;
            case "profiles":
                ProfileComboBox.Focus();
                SetStatus("Profile selector focused. Changing it does not apply system changes.", false);
                break;
            case "settings":
                SettingsSection.BringIntoView();
                SetStatus("Settings selected.", false);
                break;
        }
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _settings = _settings with
        {
            AutomaticRecommendations = AutomaticRecommendationsCheckBox.IsChecked == true,
            ShowExperimentalDefinitions = ShowExperimentalCheckBox.IsChecked == true,
            ReducedMotion = ReducedMotionCheckBox.IsChecked == true
        };
        _settingsStore.Save(_settings);
        SetStatus("Settings saved locally. No system changes were made.", false);
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = false;
        SetStatus("Scanning system, power plans, startup locations, network adapters, and local optimization definitions…", false);
        try
        {
            var scan = await Task.Run(() =>
            {
                var snapshot = _systemProfiler.Capture();
                var plans = _powerPlanProvider.ListPlans();
                var active = _powerPlanProvider.GetActivePlan();
                var definitions = _catalog.LoadAll();
                var startupItems = _startupInventory.Scan();
                var networkAdapters = _networkDiagnostics.Scan();
                IReadOnlyList<OptimizationRecommendation> recommendations = _settings.AutomaticRecommendations
                    ? _recommendationEngine.Build(
                        definitions,
                        new RecommendationContext(
                            plans.Count > 1,
                            startupItems.Count,
                            false,
                            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Power management", "Startup", "Networking" }))
                    : Array.Empty<OptimizationRecommendation>();
                return (Snapshot: snapshot, Plans: plans, Active: active, Definitions: definitions, StartupItems: startupItems, NetworkAdapters: networkAdapters, Recommendations: recommendations);
            });

            var visibleDefinitions = scan.Definitions
                .Where(definition => _settings.ShowExperimentalDefinitions || definition.Tier != QualityTier.Experimental)
                .ToArray();
            CatalogList.ItemsSource = visibleDefinitions
                .Select(definition => $"{definition.Name}  •  {definition.Tier}  •  {definition.Risk}")
                .Concat(scan.Recommendations.Select(recommendation => $"REVIEW  •  {recommendation.Title}  •  {recommendation.Confidence}"))
                .ToArray();
            StartupList.ItemsSource = scan.StartupItems.Count == 0
                ? new[] { "No startup entries detected in the supported locations." }
                : scan.StartupItems
                    .Select(item => $"{item.Name}  •  {item.Source}  •  {item.EstimatedImpact}")
                    .ToArray();
            NetworkList.ItemsSource = scan.NetworkAdapters.Count == 0
                ? new[] { "No network adapters detected." }
                : scan.NetworkAdapters
                    .Select(adapter => $"{adapter.Name}  •  {adapter.Type}  •  {adapter.Status}  •  {adapter.Addresses.Count} addresses")
                    .ToArray();
            PowerPlanComboBox.ItemsSource = scan.Plans;
            PowerPlanComboBox.SelectedItem = scan.Plans.FirstOrDefault(plan => plan.Guid == scan.Active?.Guid);
            ActivePlanText.Text = scan.Active is null ? "Unable to detect" : $"{scan.Active.Name} ({scan.Active.Guid})";
            CpuValue.Text = scan.Snapshot.CpuLogicalProcessors.ToString();
            MemoryValue.Text = FormatBytes(scan.Snapshot.AvailableMemoryBytes);
            StorageValue.Text = scan.Snapshot.Volumes.Count.ToString();
            SetStatus($"Scan complete: {scan.Snapshot.CpuLogicalProcessors} logical processors, {FormatBytes(scan.Snapshot.AvailableMemoryBytes)} available memory, {scan.Plans.Count} power plans, {scan.StartupItems.Count} startup entries, {scan.NetworkAdapters.Count} adapters, {scan.Recommendations.Count} review items.", false);
            PlanDetailsText.Text = FormatSnapshot(scan.Snapshot);
        }
        catch (Exception ex)
        {
            SetStatus($"Scan skipped: {ex.Message}", true);
        }
        finally
        {
            ScanButton.IsEnabled = true;
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

    private void DryRunButton_Click(object sender, RoutedEventArgs e)
    {
        if (PowerPlanComboBox.SelectedItem is not PowerPlan target)
        {
            SetStatus("Choose a target power plan first.", true);
            return;
        }

        try
        {
            _lastPlan = _powerPlanService.Preview(target.Guid);
            var result = _powerPlanService.Execute(_lastPlan, OperationMode.DryRun);
            PlanDetailsText.Text = result.Message;
            SetStatus("Dry run complete. No system change was made and nothing was logged as applied.", false);
        }
        catch (Exception ex)
        {
            SetStatus($"Dry run skipped: {ex.Message}", true);
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

            var definition = _catalog.LoadAll()
                .SingleOrDefault(candidate => candidate.Id.Equals("power-plan.active", StringComparison.OrdinalIgnoreCase));
            if (definition is null)
            {
                SetStatus("Change blocked: the catalog definition is unavailable.", true);
                return;
            }

            var authorization = _authorizationPolicy.Validate(
                definition,
                OperationMode.Apply,
                new AuthorizationContext(_securityContext.IsAdministrator, UserConfirmed: true, IsDryRun: false));
            if (!authorization.IsValid)
            {
                SetStatus($"Change blocked: {authorization.Reason}", true);
                return;
            }

            var result = _powerPlanService.Execute(_lastPlan, OperationMode.Apply);
            WriteRecord(_lastPlan, result, definition.Id);
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
            _benchmarkJournal.Append(new BenchmarkRecord
            {
                SessionId = Guid.NewGuid(),
                Name = sample.Name,
                CapturedUtc = sample.CapturedUtc,
                Duration = sample.Duration,
                WorkUnits = sample.WorkUnits,
                WorkUnitsPerSecond = sample.WorkUnitsPerSecond,
                RelatedOptimizationId = _lastPlan is null ? null : "power-plan.active"
            });
            BenchmarkText.Text = $"{sample.WorkUnitsPerSecond:N0} work units/sec ({sample.Duration.TotalMilliseconds:N0} ms). Saved locally.";
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

    private void RestoreLastPlanFromHistory()
    {
        var latest = _journal.GetRecent(50)
            .FirstOrDefault(record => record.OptimizationId is "power-plan.active" or "power-plan.active.rollback");
        if (latest is null || latest.OptimizationId == "power-plan.active.rollback" || latest.Result != "Verified")
        {
            return;
        }

        try
        {
            var plans = _powerPlanProvider.ListPlans();
            var before = plans.FirstOrDefault(plan => plan.Guid.Equals(latest.Before, StringComparison.OrdinalIgnoreCase));
            var target = plans.FirstOrDefault(plan => plan.Guid.Equals(latest.Requested, StringComparison.OrdinalIgnoreCase));
            if (before is not null && target is not null)
            {
                _lastPlan = new PowerPlanChangePlan(before, target);
            }
        }
        catch
        {
            // Recovery is best effort; the normal scan can reconstruct the state later.
        }
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
