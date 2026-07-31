using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class TimeWindowRequirementEvaluator
    : RequirementEvaluator<TimeWindowRequirementDefinition>
{
    protected override ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            TimeWindowRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var localTime = TimeZoneInfo.ConvertTime(
            context.AuthorizationContext.EvaluationTime,
            requirement.TimeZone);

        var day = localTime.DayOfWeek;
        var time = localTime.TimeOfDay;

        var isSatisfied = requirement.CrossesMidnight
            ? IsOvernightWindowSatisfied(
                requirement,
                day,
                time)
            : requirement.Days.Contains(day) &&
              time >= requirement.Start &&
              time < requirement.End;

        return ValueTaskCompat.FromResult(
            isSatisfied
                ? RequirementEvaluationResult.Satisfied()
                : RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .TimeWindowNotSatisfied,
                        requirement.Id)));
    }

    private static bool IsOvernightWindowSatisfied(
        TimeWindowRequirementDefinition requirement,
        DayOfWeek day,
        TimeSpan time)
    {
        if (time >= requirement.Start)
        {
            return requirement.Days.Contains(day);
        }

        if (time >= requirement.End)
        {
            return false;
        }

        var previousDay = day == DayOfWeek.Sunday
            ? DayOfWeek.Saturday
            : day - 1;

        return requirement.Days.Contains(
            previousDay);
    }
}
