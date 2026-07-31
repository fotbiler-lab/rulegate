using System.Diagnostics;
using System.Diagnostics.Metrics;
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Diagnostics;

internal static class RuleGateInstrumentation
{
    private static readonly ActivitySource ActivitySource =
        new(RuleGateTelemetry.ActivitySourceName);

    private static readonly Meter Meter =
        new(RuleGateTelemetry.MeterName);

    private static readonly Counter<long> AuthorizationEvaluations =
        Meter.CreateCounter<long>(
            "rulegate.authorization.evaluations",
            unit: "{evaluation}",
            description: "Authorization evaluations completed by RuleGate.");

    private static readonly Histogram<double> AuthorizationDuration =
        Meter.CreateHistogram<double>(
            "rulegate.authorization.duration",
            unit: "s",
            description: "RuleGate authorization evaluation duration.");

    private static readonly Counter<long> PolicyLookups =
        Meter.CreateCounter<long>(
            "rulegate.policy.lookups",
            unit: "{lookup}",
            description: "Policy lookups completed by RuleGate.");

    private static readonly Histogram<double> PolicyLookupDuration =
        Meter.CreateHistogram<double>(
            "rulegate.policy.lookup.duration",
            unit: "s",
            description: "RuleGate policy lookup duration.");

    private static readonly Counter<long> PolicyReloads =
        Meter.CreateCounter<long>(
            "rulegate.policy.reloads",
            unit: "{reload}",
            description: "Policy reload attempts completed by RuleGate.");

    private static readonly Histogram<double> PolicyReloadDuration =
        Meter.CreateHistogram<double>(
            "rulegate.policy.reload.duration",
            unit: "s",
            description: "RuleGate policy reload duration.");

    private static readonly Counter<long> PolicySourceLoads =
        Meter.CreateCounter<long>(
            "rulegate.policy.source.loads",
            unit: "{load}",
            description: "Policy source load attempts completed by RuleGate.");

    private static readonly Histogram<double> PolicySourceLoadDuration =
        Meter.CreateHistogram<double>(
            "rulegate.policy.source.load.duration",
            unit: "s",
            description: "RuleGate policy source load duration.");

    private static readonly Histogram<long> ActivatedPolicyCount =
        Meter.CreateHistogram<long>(
            "rulegate.policy.snapshot.policy_count",
            unit: "{policy}",
            description: "Policy count in an activated RuleGate snapshot.");

    public static Activity? StartAuthorizationActivity()
    {
        return ActivitySource.StartActivity(
            RuleGateTelemetry.AuthorizationActivityName,
            ActivityKind.Internal);
    }

    public static Activity? StartPolicyReloadActivity()
    {
        return ActivitySource.StartActivity(
            RuleGateTelemetry.PolicyReloadActivityName,
            ActivityKind.Internal);
    }

    public static Activity? StartPolicySourceLoadActivity()
    {
        return ActivitySource.StartActivity(
            RuleGateTelemetry.PolicySourceLoadActivityName,
            ActivityKind.Internal);
    }

    public static void RecordAuthorization(
        Activity? activity,
        long startTimestamp,
        bool isAllowed,
        bool policyMatched,
        RequirementEvaluationOutcome? requirementOutcome)
    {
        var decision = isAllowed ? "allow" : "deny";
        var failureCategory = GetFailureCategory(
            isAllowed,
            policyMatched,
            requirementOutcome);

        var tags = new TagList
        {
            { "rulegate.authorization.outcome", decision },
            { "rulegate.authorization.failure_category", failureCategory },
            { "rulegate.policy.matched", policyMatched },
        };

        AuthorizationEvaluations.Add(1, tags);
        AuthorizationDuration.Record(
            Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds,
            tags);
        SetTags(activity, tags);
    }

    public static void RecordAuthorizationCancellation(
        Activity? activity,
        long startTimestamp)
    {
        RecordAuthorizationTerminalState(
            activity,
            startTimestamp,
            "cancelled");
    }

    public static void RecordAuthorizationError(
        Activity? activity,
        long startTimestamp)
    {
        RecordAuthorizationTerminalState(
            activity,
            startTimestamp,
            "error");
    }

    public static void RecordPolicyLookup(
        long startTimestamp,
        bool policyMatched)
    {
        var tags = new TagList
        {
            { "rulegate.policy.matched", policyMatched },
        };

        PolicyLookups.Add(1, tags);
        PolicyLookupDuration.Record(
            Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds,
            tags);
    }

    public static void RecordPolicyReload(
        Activity? activity,
        long startTimestamp,
        PolicyReloadResult result)
    {
        var reloadResult = result.IsActivated
            ? "activated"
            : "rejected";

        var tags = new TagList
        {
            { "rulegate.policy.reload.result", reloadResult },
        };

        PolicyReloads.Add(1, tags);
        PolicyReloadDuration.Record(
            Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds,
            tags);

        activity?.SetTag(
            "rulegate.policy.reload.result",
            reloadResult);

        if (result.IsActivated)
        {
            ActivatedPolicyCount.Record(
                result.ActiveSnapshot.PolicyCount);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }
    }

    public static void RecordPolicyReloadCancellation(
        Activity? activity,
        long startTimestamp)
    {
        RecordPolicyReloadTerminalState(
            activity,
            startTimestamp,
            "cancelled",
            isError: true);
    }

    public static void RecordPolicyReloadCoalesced(
        Activity? activity,
        long startTimestamp)
    {
        RecordPolicyReloadTerminalState(
            activity,
            startTimestamp,
            "coalesced",
            isError: false);
    }

    public static void RecordPolicyReloadError(
        Activity? activity,
        long startTimestamp)
    {
        RecordPolicyReloadTerminalState(
            activity,
            startTimestamp,
            "error",
            isError: true);
    }

    public static void RecordPolicySourceLoad(
        Activity? activity,
        long startTimestamp,
        string result)
    {
        var tags = new TagList
        {
            { "rulegate.policy.source.load.result", result },
        };

        PolicySourceLoads.Add(1, tags);
        PolicySourceLoadDuration.Record(
            Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds,
            tags);
        SetTags(activity, tags);

        if (!string.Equals(
                result,
                "success",
                StringComparison.Ordinal))
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }
    }

    private static void RecordAuthorizationTerminalState(
        Activity? activity,
        long startTimestamp,
        string state)
    {
        var tags = new TagList
        {
            { "rulegate.authorization.outcome", state },
            { "rulegate.authorization.failure_category", state },
            { "rulegate.policy.matched", false },
        };

        AuthorizationEvaluations.Add(1, tags);
        AuthorizationDuration.Record(
            Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds,
            tags);
        SetTags(activity, tags);
        activity?.SetStatus(ActivityStatusCode.Error);
    }

    private static void RecordPolicyReloadTerminalState(
        Activity? activity,
        long startTimestamp,
        string state,
        bool isError)
    {
        var tags = new TagList
        {
            { "rulegate.policy.reload.result", state },
        };

        PolicyReloads.Add(1, tags);
        PolicyReloadDuration.Record(
            Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds,
            tags);
        SetTags(activity, tags);

        if (isError)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }
    }

    private static string GetFailureCategory(
        bool isAllowed,
        bool policyMatched,
        RequirementEvaluationOutcome? requirementOutcome)
    {
        if (isAllowed)
        {
            return "none";
        }

        if (!policyMatched)
        {
            return "no_matching_policy";
        }

        return requirementOutcome ==
            RequirementEvaluationOutcome.Indeterminate
                ? "indeterminate"
                : "not_satisfied";
    }

    private static void SetTags(
        Activity? activity,
        in TagList tags)
    {
        if (activity is null)
        {
            return;
        }

        foreach (var tag in tags)
        {
            activity.SetTag(tag.Key, tag.Value);
        }
    }
}
