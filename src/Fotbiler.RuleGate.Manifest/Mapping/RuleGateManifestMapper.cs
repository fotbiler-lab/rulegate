using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Parsing;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Mapping;

public sealed class RuleGateManifestMapper
{
    private readonly RuleGateManifestValidator _validator;

    public RuleGateManifestMapper(
        RuleGateManifestValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        _validator = validator;
    }

    public ManifestMappingResult Map(
        RuleGateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var validationResult =
            _validator.Validate(manifest);

        if (!validationResult.IsValid)
        {
            return ManifestMappingResult.Failure(
                validationResult.Errors);
        }

        var policies = manifest.Policies!
            .Select(static policy =>
                MapPolicy(policy!))
            .ToArray();

        return ManifestMappingResult.Success(
            policies);
    }

    private static PolicyDefinition MapPolicy(
        ManifestPolicy policy)
    {
        return new PolicyDefinition(
            id: policy.Id!,
            resourceType: policy.ResourceType!,
            action: policy.Action!,
            requirement:
                MapRequirement(
                    policy.Requirement!));
    }

    private static RequirementDefinition MapRequirement(
        ManifestRequirement requirement)
    {
        if (requirement.Permission is not null)
        {
            return new PermissionRequirementDefinition(
                permission: requirement.Permission,
                id: requirement.Id);
        }

        if (requirement.Role is not null)
        {
            return new RoleRequirementDefinition(
                role: requirement.Role,
                id: requirement.Id);
        }

        if (requirement.Attribute is not null)
        {
            var attribute =
                requirement.Attribute;

            ManifestAttributeRequirementConversions
                .TryParseSource(
                    attribute.Source,
                    out var source);

            ManifestAttributeRequirementConversions
                .TryParseOperator(
                    attribute.Operator,
                    out var @operator);

            object? value = null;

            if (ManifestAttributeRequirementConversions
                .OperatorRequiresValue(@operator))
            {
                ManifestAttributeRequirementConversions
                    .TryConvertValue(
                        attribute,
                        out value);
            }

            ManifestAttributeRequirementConversions
                .TryParseStringComparison(
                    attribute.StringComparison,
                    out var stringComparison);

            return new AttributeRequirementDefinition(
                source,
                attribute.Name!,
                @operator,
                value,
                requirement.Id,
                stringComparison);
        }

        if (requirement.AttributeComparison is not null)
        {
            var comparison =
                requirement.AttributeComparison;

            ManifestAttributeRequirementConversions
                .TryParseOperator(
                    comparison.Operator,
                    out var @operator);

            ManifestAttributeRequirementConversions
                .TryParseStringComparison(
                    comparison.StringComparison,
                    out var stringComparison);

            return new AttributeComparisonRequirementDefinition(
                MapOperand(comparison.Left!),
                @operator,
                MapOperand(comparison.Right!),
                requirement.Id,
                stringComparison);
        }

        if (requirement.TimeWindow is not null)
        {
            var timeWindow = requirement.TimeWindow;

            var days = timeWindow.Days!
                .Select(static value =>
                {
                    ManifestTimeContextConversions.TryParseDay(
                        value,
                        out var day);
                    return day;
                });

            ManifestTimeContextConversions.TryParseTime(
                timeWindow.Start,
                out var start);

            ManifestTimeContextConversions.TryParseTime(
                timeWindow.End,
                out var end);

            ManifestTimeContextConversions.TryParseTimeZone(
                timeWindow.TimeZone,
                out var timeZone);

            return new TimeWindowRequirementDefinition(
                days,
                start,
                end,
                timeZone!,
                requirement.Id);
        }

        if (requirement.DateTimeWindow is not null)
        {
            var dateTimeWindow = requirement.DateTimeWindow;

            DateTimeOffset? startsAt = null;
            DateTimeOffset? endsAt = null;

            if (dateTimeWindow.StartsAt is not null)
            {
                ManifestTimeContextConversions.TryParseDateTimeOffset(
                    dateTimeWindow.StartsAt,
                    out var parsedStartsAt);
                startsAt = parsedStartsAt;
            }

            if (dateTimeWindow.EndsAt is not null)
            {
                ManifestTimeContextConversions.TryParseDateTimeOffset(
                    dateTimeWindow.EndsAt,
                    out var parsedEndsAt);
                endsAt = parsedEndsAt;
            }

            return new DateTimeWindowRequirementDefinition(
                startsAt,
                endsAt,
                requirement.Id);
        }

        if (requirement.ContextAge is not null)
        {
            var contextAge = requirement.ContextAge;

            ManifestTimeContextConversions
                .TryParseContextTimestamp(
                    contextAge.Timestamp,
                    out var timestamp);

            ManifestTimeContextConversions.TryParseMaximumAge(
                contextAge.MaximumAge,
                out var maximumAge);

            return new ContextAgeRequirementDefinition(
                timestamp,
                maximumAge,
                requirement.Id);
        }

        if (requirement.Context is not null)
        {
            var context = requirement.Context;

            ManifestTimeContextConversions.TryParseContextProperty(
                context.Property,
                out var property);

            ManifestAttributeRequirementConversions.TryParseOperator(
                context.Operator,
                out var @operator);

            ManifestAttributeRequirementConversions.TryConvertValue(
                context.ValueType,
                context.Value,
                context.HasValue,
                out var value);

            ManifestAttributeRequirementConversions
                .TryParseStringComparison(
                    context.StringComparison,
                    out var stringComparison);

            return new ContextRequirementDefinition(
                property,
                @operator,
                value!,
                requirement.Id,
                stringComparison);
        }

        if (requirement.All is not null)
        {
            return new AllRequirementDefinition(
                requirements:
                    requirement.All.Select(
                        static child =>
                            MapRequirement(child!)),
                id: requirement.Id);
        }

        if (requirement.Any is not null)
        {
            return new AnyRequirementDefinition(
                requirements:
                    requirement.Any.Select(
                        static child =>
                            MapRequirement(child!)),
                id: requirement.Id);
        }

        return new NotRequirementDefinition(
            requirement:
                MapRequirement(requirement.Not!),
            id: requirement.Id);
    }

    private static AuthorizationAttributeOperand MapOperand(
        ManifestAttributeComparisonOperand operand)
    {
        if (operand.Source is not null ||
            operand.Name is not null)
        {
            ManifestAttributeRequirementConversions
                .TryParseSource(
                    operand.Source,
                    out var source);

            return AuthorizationAttributeOperand.Attribute(
                source,
                operand.Name!);
        }

        ManifestAttributeRequirementConversions
            .TryConvertValue(
                operand.ValueType,
                operand.Value,
                operand.HasValue,
                out var value);

        return AuthorizationAttributeOperand.Literal(value);
    }
}
