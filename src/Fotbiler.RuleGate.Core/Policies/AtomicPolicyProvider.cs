using System.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Diagnostics;

namespace Fotbiler.RuleGate.Core.Policies;

public sealed class AtomicPolicyProvider :
    IPolicyProvider,
    IPolicyReloadService
{
    private readonly IReadOnlyList<IPolicySource> _sources;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    private PolicySnapshot _snapshot = PolicySnapshot.Empty;
    private PolicyReloadResult _lastReload;
    private int _initializationAttempted;

    public AtomicPolicyProvider(
        IEnumerable<IPolicySource> sources,
        IEnumerable<PolicyDefinition>? inMemoryPolicies = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var items = sources.ToList();

        if (items.Any(static source => source is null))
        {
            throw new ArgumentException(
                "Policy sources cannot contain null values.",
                nameof(sources));
        }

        if (inMemoryPolicies is not null)
        {
            var policies = inMemoryPolicies.ToArray();

            if (policies.Length > 0)
            {
                items.Add(new InMemoryPolicySource(policies));
            }
        }

        _sources = items.AsReadOnly();
        _lastReload = PolicyReloadResult.Activated(
            PolicySnapshot.Empty.Info);
    }

    public PolicySnapshotInfo CurrentSnapshot =>
        Volatile.Read(ref _snapshot).Info;

    public bool HasReloaded =>
        Volatile.Read(ref _initializationAttempted) != 0;

    public PolicyReloadResult LastReload =>
        Volatile.Read(ref _lastReload);

    public async ValueTask<PolicyDefinition?> FindAsync(
        string resourceType,
        string action,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureInitializedAsync(cancellationToken);

        return Volatile.Read(ref _snapshot).Find(
            resourceType,
            action);
    }

    public async ValueTask<PolicyReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default)
    {
        using var activity =
            RuleGateInstrumentation
                .StartPolicyReloadActivity();

        var startTimestamp = Stopwatch.GetTimestamp();
        var lockTaken = false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _reloadLock.WaitAsync(cancellationToken);
            lockTaken = true;

            var result = await LoadAndActivateAsync(
                cancellationToken);

            Volatile.Write(ref _lastReload, result);
            Volatile.Write(ref _initializationAttempted, 1);

            RuleGateInstrumentation.RecordPolicyReload(
                activity,
                startTimestamp,
                result);

            return result;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            RuleGateInstrumentation
                .RecordPolicyReloadCancellation(
                    activity,
                    startTimestamp);

            throw;
        }
        catch (Exception)
        {
            RuleGateInstrumentation.RecordPolicyReloadError(
                activity,
                startTimestamp);

            throw;
        }
        finally
        {
            if (lockTaken)
            {
                _reloadLock.Release();
            }
        }
    }

    private async ValueTask EnsureInitializedAsync(
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _initializationAttempted) != 0)
        {
            return;
        }

        using var activity =
            RuleGateInstrumentation
                .StartPolicyReloadActivity();

        var startTimestamp = Stopwatch.GetTimestamp();
        var lockTaken = false;

        try
        {
            await _reloadLock.WaitAsync(cancellationToken);
            lockTaken = true;

            if (Volatile.Read(ref _initializationAttempted) != 0)
            {
                RuleGateInstrumentation
                    .RecordPolicyReloadCoalesced(
                        activity,
                        startTimestamp);

                return;
            }

            var result = await LoadAndActivateAsync(
                cancellationToken);

            Volatile.Write(ref _lastReload, result);
            Volatile.Write(ref _initializationAttempted, 1);

            RuleGateInstrumentation.RecordPolicyReload(
                activity,
                startTimestamp,
                result);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            RuleGateInstrumentation
                .RecordPolicyReloadCancellation(
                    activity,
                    startTimestamp);

            throw;
        }
        catch (Exception)
        {
            RuleGateInstrumentation.RecordPolicyReloadError(
                activity,
                startTimestamp);

            throw;
        }
        finally
        {
            if (lockTaken)
            {
                _reloadLock.Release();
            }
        }
    }

    private async ValueTask<PolicyReloadResult>
        LoadAndActivateAsync(
            CancellationToken cancellationToken)
    {
        var diagnostics = new List<PolicyReloadDiagnostic>();
        var entries = new List<PolicyEntry>();
        var loadedSourceNames = new List<string>();

        AddDuplicateSourceDiagnostics(diagnostics);

        foreach (var source in _sources
                     .OrderBy(static item => item.Name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var sourceActivity =
                RuleGateInstrumentation
                    .StartPolicySourceLoadActivity();

            var sourceStartTimestamp =
                Stopwatch.GetTimestamp();

            PolicySourceLoadResult? loadResult;

            try
            {
                loadResult = await source.LoadAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                RuleGateInstrumentation.RecordPolicySourceLoad(
                    sourceActivity,
                    sourceStartTimestamp,
                    "cancelled");

                throw;
            }
            catch (Exception)
            {
                RuleGateInstrumentation.RecordPolicySourceLoad(
                    sourceActivity,
                    sourceStartTimestamp,
                    "error");

                diagnostics.Add(
                    new PolicyReloadDiagnostic(
                        source.Name,
                        PolicyReloadCodes.SourceLoadException,
                        "The policy source threw an exception while loading."));

                continue;
            }

            if (loadResult is null)
            {
                RuleGateInstrumentation.RecordPolicySourceLoad(
                    sourceActivity,
                    sourceStartTimestamp,
                    "invalid");

                diagnostics.Add(
                    new PolicyReloadDiagnostic(
                        source.Name,
                        PolicyReloadCodes.SourceReturnedNull,
                        "The policy source returned no load result."));

                continue;
            }

            RuleGateInstrumentation.RecordPolicySourceLoad(
                sourceActivity,
                sourceStartTimestamp,
                loadResult.IsSuccess
                    ? "success"
                    : "rejected");

            foreach (var diagnostic in loadResult.Diagnostics)
            {
                diagnostics.Add(
                    new PolicyReloadDiagnostic(
                        source.Name,
                        diagnostic.Code,
                        diagnostic.Message,
                        diagnostic.Path));
            }

            if (!loadResult.IsSuccess)
            {
                continue;
            }

            loadedSourceNames.Add(source.Name);

            entries.AddRange(
                loadResult.Policies.Select(
                    policy => new PolicyEntry(
                        source.Name,
                        policy)));
        }

        AddDuplicatePolicyDiagnostics(
            entries,
            diagnostics);

        if (diagnostics.Count > 0)
        {
            return PolicyReloadResult.Rejected(
                CurrentSnapshot,
                diagnostics);
        }

        var current = Volatile.Read(ref _snapshot);
        var snapshot = PolicySnapshot.Create(
            checked(current.Info.Version + 1),
            entries,
            loadedSourceNames);

        Volatile.Write(ref _snapshot, snapshot);

        return PolicyReloadResult.Activated(
            snapshot.Info);
    }

    private void AddDuplicateSourceDiagnostics(
        ICollection<PolicyReloadDiagnostic> diagnostics)
    {
        foreach (var group in _sources
                     .GroupBy(
                         static source => source.Name,
                         StringComparer.Ordinal)
                     .Where(static group => group.Count() > 1)
                     .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            diagnostics.Add(
                new PolicyReloadDiagnostic(
                    group.Key,
                    PolicyReloadCodes.DuplicateSourceName,
                    $"Multiple policy sources use the name '{group.Key}'."));
        }
    }

    private static void AddDuplicatePolicyDiagnostics(
        IEnumerable<PolicyEntry> entries,
        ICollection<PolicyReloadDiagnostic> diagnostics)
    {
        var ordered = entries
            .OrderBy(static item => item.SourceName, StringComparer.Ordinal)
            .ThenBy(static item => item.Policy.Id, StringComparer.Ordinal)
            .ThenBy(static item => item.Policy.ResourceType, StringComparer.Ordinal)
            .ThenBy(static item => item.Policy.Action, StringComparer.Ordinal)
            .ToArray();

        foreach (var group in ordered
                     .GroupBy(
                         static item => item.Policy.Id,
                         StringComparer.Ordinal)
                     .Where(static group => group.Count() > 1)
                     .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            var sourceName = group.First().SourceName;

            diagnostics.Add(
                new PolicyReloadDiagnostic(
                    sourceName,
                    PolicyReloadCodes.DuplicatePolicyId,
                    $"Multiple policies use the identifier '{group.Key}'.",
                    $"policies.{group.Key}"));
        }

        foreach (var group in ordered
                     .GroupBy(
                         static item => new PolicyRoute(
                             item.Policy.ResourceType,
                             item.Policy.Action))
                     .Where(static group => group.Count() > 1)
                     .OrderBy(static group => group.Key.ResourceType, StringComparer.Ordinal)
                     .ThenBy(static group => group.Key.Action, StringComparer.Ordinal))
        {
            var sourceName = group.First().SourceName;

            diagnostics.Add(
                new PolicyReloadDiagnostic(
                    sourceName,
                    PolicyReloadCodes.DuplicatePolicyRoute,
                    $"Multiple policies target resource type '{group.Key.ResourceType}' and action '{group.Key.Action}'.",
                    $"routes.{group.Key.ResourceType}.{group.Key.Action}"));
        }
    }

    private sealed class PolicySnapshot
    {
        private readonly Dictionary<PolicyRoute, PolicyDefinition>
            _policies;

        private PolicySnapshot(
            PolicySnapshotInfo info,
            Dictionary<PolicyRoute, PolicyDefinition> policies)
        {
            Info = info;
            _policies = policies;
        }

        public static PolicySnapshot Empty { get; } =
            new(
                new PolicySnapshotInfo(0, 0, []),
                new Dictionary<PolicyRoute, PolicyDefinition>());

        public PolicySnapshotInfo Info { get; }

        public PolicyDefinition? Find(
            string resourceType,
            string action)
        {
            return _policies.TryGetValue(
                new PolicyRoute(resourceType, action),
                out var policy)
                ? policy
                : null;
        }

        public static PolicySnapshot Create(
            long version,
            IEnumerable<PolicyEntry> entries,
            IEnumerable<string> sourceNames)
        {
            var items = entries.ToArray();
            var policies = items.ToDictionary(
                static item => new PolicyRoute(
                    item.Policy.ResourceType,
                    item.Policy.Action),
                static item => item.Policy);

            var names = sourceNames
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();

            return new PolicySnapshot(
                new PolicySnapshotInfo(
                    version,
                    policies.Count,
                    names),
                policies);
        }
    }

    private sealed record PolicyEntry(
        string SourceName,
        PolicyDefinition Policy);

    private readonly record struct PolicyRoute(
        string ResourceType,
        string Action);
}
