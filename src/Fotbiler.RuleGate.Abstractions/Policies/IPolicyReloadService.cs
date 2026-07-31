namespace Fotbiler.RuleGate.Abstractions.Policies;

public interface IPolicyReloadService
{
    bool HasReloaded { get; }

    PolicySnapshotInfo CurrentSnapshot { get; }

    PolicyReloadResult LastReload { get; }

    ValueTask<PolicyReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default);
}
