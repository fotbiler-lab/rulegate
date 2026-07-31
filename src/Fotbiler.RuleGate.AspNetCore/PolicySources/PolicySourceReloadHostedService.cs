using System.Threading.Channels;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.PolicySources;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Fotbiler.RuleGate.AspNetCore.PolicySources;

internal sealed class PolicySourceReloadHostedService : BackgroundService
{
    private static readonly EventId SnapshotActivatedEvent =
        new(2020, "PolicySnapshotActivated");

    private static readonly EventId ReloadRejectedEvent =
        new(2021, "PolicyReloadRejected");

    private static readonly EventId WatchUnavailableEvent =
        new(2022, "PolicySourceWatchUnavailable");

    private static readonly TimeSpan ReloadDebounce =
        TimeSpan.FromMilliseconds(200);

    private readonly IPolicyReloadService _reloadService;
    private readonly IReadOnlyList<YamlFilePolicySource>
        _yamlSources;
    private readonly IReadOnlyList<ConfigurationPolicySource>
        _configurationSources;
    private readonly ILogger<PolicySourceReloadHostedService> _logger;
    private readonly Channel<byte> _reloadRequests =
        Channel.CreateBounded<byte>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false,
            });

    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<FileSystemWatcher> _watchers = [];

    public PolicySourceReloadHostedService(
        IPolicyReloadService reloadService,
        IEnumerable<YamlFilePolicySource> yamlSources,
        IEnumerable<ConfigurationPolicySource> configurationSources,
        ILogger<PolicySourceReloadHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(reloadService);
        ArgumentNullException.ThrowIfNull(yamlSources);
        ArgumentNullException.ThrowIfNull(configurationSources);
        ArgumentNullException.ThrowIfNull(logger);

        _reloadService = reloadService;
        _yamlSources = yamlSources.ToArray();
        _configurationSources = configurationSources.ToArray();
        _logger = logger;
    }

    public override async Task StartAsync(
        CancellationToken cancellationToken)
    {
        RegisterChangeMonitors();

        await ReloadAndLogAsync(cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await foreach (var request in _reloadRequests.Reader
                           .ReadAllAsync(stoppingToken))
        {
            _ = request;

            await Task.Delay(
                ReloadDebounce,
                stoppingToken);

            while (_reloadRequests.Reader.TryRead(out var pendingRequest))
            {
                _ = pendingRequest;
            }

            await ReloadAndLogAsync(stoppingToken);
        }
    }

    public override void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        base.Dispose();
    }

    private void RegisterChangeMonitors()
    {
        foreach (var source in _yamlSources
                     .Where(static source => source.ReloadOnChange))
        {
            RegisterYamlWatcher(source);
        }

        foreach (var source in _configurationSources
                     .Where(static source => source.ReloadOnChange))
        {
            _subscriptions.Add(
                ChangeToken.OnChange(
                    source.GetReloadToken,
                    QueueReload));
        }
    }

    private void RegisterYamlWatcher(
        YamlFilePolicySource source)
    {
        var directory = Path.GetDirectoryName(
            source.FullPath);

        if (string.IsNullOrWhiteSpace(directory) ||
            !Directory.Exists(directory))
        {
            _logger.LogWarning(
                WatchUnavailableEvent,
                "RuleGate policy source {PolicySource} cannot be watched because its directory is unavailable.",
                source.Name);

            return;
        }

        var watcher = new FileSystemWatcher(
            directory,
            Path.GetFileName(source.FullPath))
        {
            IncludeSubdirectories = false,
            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.CreationTime,
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
        watcher.EnableRaisingEvents = true;

        _watchers.Add(watcher);
    }

    private async Task ReloadAndLogAsync(
        CancellationToken cancellationToken)
    {
        var result = await _reloadService.ReloadAsync(
            cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                SnapshotActivatedEvent,
                "RuleGate activated policy snapshot {SnapshotVersion} with {PolicyCount} policies.",
                result.ActiveSnapshot.Version,
                result.ActiveSnapshot.PolicyCount);

            return;
        }

        _logger.LogWarning(
            ReloadRejectedEvent,
            "RuleGate rejected a policy reload and preserved snapshot {SnapshotVersion}. Diagnostic codes: {DiagnosticCodes}",
            result.ActiveSnapshot.Version,
            string.Join(
                ",",
                result.Diagnostics.Select(
                    static diagnostic => diagnostic.Code)));
    }

    private void OnFileChanged(
        object sender,
        FileSystemEventArgs args)
    {
        QueueReload();
    }

    private void OnFileRenamed(
        object sender,
        RenamedEventArgs args)
    {
        QueueReload();
    }

    private void QueueReload()
    {
        _reloadRequests.Writer.TryWrite(0);
    }
}
