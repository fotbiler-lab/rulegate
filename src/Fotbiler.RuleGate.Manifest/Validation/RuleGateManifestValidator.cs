using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Parsing;

namespace Fotbiler.RuleGate.Manifest.Validation;

public sealed class RuleGateManifestValidator
{
    public ManifestValidationResult Validate(
        RuleGateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var errors =
            new List<ManifestValidationError>();

        ValidateSchemaVersion(
            manifest.SchemaVersion,
            errors);

        ValidateApplication(
            manifest.Application,
            errors);

        ValidatePolicies(
            manifest.Policies,
            errors);

        return new ManifestValidationResult(errors);
    }

    private static void ValidateSchemaVersion(
        int schemaVersion,
        ICollection<ManifestValidationError> errors)
    {
        if (schemaVersion ==
            RuleGateManifestDefaults.SupportedSchemaVersion)
        {
            return;
        }

        errors.Add(
            new ManifestValidationError(
                ManifestValidationCodes
                    .UnsupportedSchemaVersion,
                "schemaVersion",
                $"Schema version '{schemaVersion}' is not supported."));
    }

    private static void ValidateApplication(
        ManifestApplication? application,
        ICollection<ManifestValidationError> errors)
    {
        if (application is null)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .ApplicationRequired,
                    "application",
                    "Application configuration is required."));

            return;
        }

        if (string.IsNullOrWhiteSpace(application.Id))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .ApplicationIdRequired,
                    "application.id",
                    "Application identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(application.Name))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .ApplicationNameRequired,
                    "application.name",
                    "Application name is required."));
        }
    }

    private static void ValidatePolicies(
        IReadOnlyList<ManifestPolicy?>? policies,
        ICollection<ManifestValidationError> errors)
    {
        if (policies is null)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .PoliciesRequired,
                    "policies",
                    "The policies collection is required."));

            return;
        }

        var identifiers =
            new HashSet<string>(StringComparer.Ordinal);

        var routes =
            new HashSet<PolicyRoute>();

        for (var index = 0;
             index < policies.Count;
             index++)
        {
            var path = $"policies[{index}]";
            var policy = policies[index];

            if (policy is null)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .PolicyRequired,
                        path,
                        "Policy definition is required."));

                continue;
            }

            ValidatePolicy(
                policy,
                path,
                identifiers,
                routes,
                errors);
        }
    }

    private static void ValidatePolicy(
        ManifestPolicy policy,
        string path,
        ISet<string> identifiers,
        ISet<PolicyRoute> routes,
        ICollection<ManifestValidationError> errors)
    {
        var hasId =
            !string.IsNullOrWhiteSpace(policy.Id);

        var hasResourceType =
            !string.IsNullOrWhiteSpace(
                policy.ResourceType);

        var hasAction =
            !string.IsNullOrWhiteSpace(policy.Action);

        if (!hasId)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .PolicyIdRequired,
                    $"{path}.id",
                    "Policy identifier is required."));
        }
        else if (!identifiers.Add(policy.Id!))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .DuplicatePolicyId,
                    $"{path}.id",
                    $"Policy identifier '{policy.Id}' is duplicated."));
        }

        if (!hasResourceType)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .PolicyResourceTypeRequired,
                    $"{path}.resourceType",
                    "Policy resource type is required."));
        }

        if (!hasAction)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .PolicyActionRequired,
                    $"{path}.action",
                    "Policy action is required."));
        }

        if (hasResourceType && hasAction)
        {
            var route = new PolicyRoute(
                policy.ResourceType!,
                policy.Action!);

            if (!routes.Add(route))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .DuplicatePolicyRoute,
                        path,
                        $"A policy already exists for resource type '{policy.ResourceType}' and action '{policy.Action}'."));
            }
        }

        if (policy.Requirement is null)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .PolicyRequirementRequired,
                    $"{path}.requirement",
                    "Policy requirement is required."));

            return;
        }

        ValidateRequirement(
            policy.Requirement,
            $"{path}.requirement",
            errors);
    }

    private static void ValidateRequirement(
        ManifestRequirement requirement,
        string path,
        ICollection<ManifestValidationError> errors)
    {
        if (requirement.Id is not null &&
            string.IsNullOrWhiteSpace(requirement.Id))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .RequirementIdInvalid,
                    $"{path}.id",
                    "Requirement identifier cannot be empty."));
        }

        var kindCount = 0;

        kindCount += requirement.Permission is null
            ? 0
            : 1;

        kindCount += requirement.Role is null
            ? 0
            : 1;

        kindCount += requirement.Attribute is null
            ? 0
            : 1;

        kindCount += requirement.AttributeComparison is null
            ? 0
            : 1;

        kindCount += requirement.TimeWindow is null
            ? 0
            : 1;

        kindCount += requirement.DateTimeWindow is null
            ? 0
            : 1;

        kindCount += requirement.ContextAge is null
            ? 0
            : 1;

        kindCount += requirement.Context is null
            ? 0
            : 1;

        kindCount += requirement.All is null
            ? 0
            : 1;

        kindCount += requirement.Any is null
            ? 0
            : 1;

        kindCount += requirement.Not is null
            ? 0
            : 1;

        if (kindCount != 1)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .RequirementKindInvalid,
                    path,
                    "A requirement must define exactly one of permission, role, attribute, attributeComparison, timeWindow, dateTimeWindow, contextAge, context, all, any, or not."));
        }

        if (requirement.Permission is not null &&
            string.IsNullOrWhiteSpace(
                requirement.Permission))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .PermissionRequired,
                    $"{path}.permission",
                    "Permission value is required."));
        }

        if (requirement.Role is not null &&
            string.IsNullOrWhiteSpace(requirement.Role))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .RoleRequired,
                    $"{path}.role",
                    "Role value is required."));
        }

        if (requirement.Attribute is not null)
        {
            ValidateAttributeRequirement(
                requirement.Attribute,
                $"{path}.attribute",
                errors);
        }

        if (requirement.AttributeComparison is not null)
        {
            ValidateAttributeComparisonRequirement(
                requirement.AttributeComparison,
                $"{path}.attributeComparison",
                errors);
        }

        if (requirement.TimeWindow is not null)
        {
            ValidateTimeWindowRequirement(
                requirement.TimeWindow,
                $"{path}.timeWindow",
                errors);
        }

        if (requirement.DateTimeWindow is not null)
        {
            ValidateDateTimeWindowRequirement(
                requirement.DateTimeWindow,
                $"{path}.dateTimeWindow",
                errors);
        }

        if (requirement.ContextAge is not null)
        {
            ValidateContextAgeRequirement(
                requirement.ContextAge,
                $"{path}.contextAge",
                errors);
        }

        if (requirement.Context is not null)
        {
            ValidateContextRequirement(
                requirement.Context,
                $"{path}.context",
                errors);
        }

        if (requirement.All is not null)
        {
            ValidateRequirementCollection(
                requirement.All,
                $"{path}.all",
                errors);
        }

        if (requirement.Any is not null)
        {
            ValidateRequirementCollection(
                requirement.Any,
                $"{path}.any",
                errors);
        }

        if (requirement.Not is not null)
        {
            ValidateRequirement(
                requirement.Not,
                $"{path}.not",
                errors);
        }
    }

    private static void ValidateAttributeRequirement(
        ManifestAttributeRequirement requirement,
        string path,
        ICollection<ManifestValidationError> errors)
    {
        var hasSource =
            !string.IsNullOrWhiteSpace(
                requirement.Source);

        var hasOperator =
            !string.IsNullOrWhiteSpace(
                requirement.Operator);

        var sourceIsValid = false;
        var operatorIsValid = false;
        var valueTypeIsValid = false;

        var parsedOperator = default(
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator);

        var parsedValueType = default(
            ManifestAttributeValueType);

        if (!hasSource)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeSourceRequired,
                    $"{path}.source",
                    "Attribute source is required."));
        }
        else
        {
            sourceIsValid =
                ManifestAttributeRequirementConversions
                    .TryParseSource(
                        requirement.Source,
                        out _);

            if (!sourceIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeSourceInvalid,
                        $"{path}.source",
                        $"Attribute source '{requirement.Source}' is not supported."));
            }
        }

        if (string.IsNullOrWhiteSpace(
                requirement.Name))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeNameRequired,
                    $"{path}.name",
                    "Attribute name is required."));
        }

        if (!hasOperator)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeOperatorRequired,
                    $"{path}.operator",
                    "Attribute operator is required."));
        }
        else
        {
            operatorIsValid =
                ManifestAttributeRequirementConversions
                    .TryParseOperator(
                        requirement.Operator,
                        out parsedOperator);

            if (!operatorIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeOperatorInvalid,
                        $"{path}.operator",
                        $"Attribute operator '{requirement.Operator}' is not supported."));
            }
        }

        var requiresValue =
            !operatorIsValid ||
            ManifestAttributeRequirementConversions
                .OperatorRequiresValue(parsedOperator);

        if (requiresValue)
        {
            if (string.IsNullOrWhiteSpace(
                    requirement.ValueType))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeValueTypeRequired,
                        $"{path}.valueType",
                        "Attribute value type is required."));
            }
            else
            {
                valueTypeIsValid =
                    ManifestAttributeRequirementConversions
                        .TryParseValueType(
                            requirement.ValueType,
                            out parsedValueType);

                if (!valueTypeIsValid)
                {
                    errors.Add(
                        new ManifestValidationError(
                            ManifestValidationCodes
                                .AttributeValueTypeInvalid,
                            $"{path}.valueType",
                            $"Attribute value type '{requirement.ValueType}' is not supported."));
                }
            }

            if (!requirement.HasValue)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeValueRequired,
                        $"{path}.value",
                        "Attribute value must be specified."));
            }
            else if (valueTypeIsValid &&
                     !ManifestAttributeRequirementConversions
                         .TryConvertValue(
                             requirement,
                             out _))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeValueInvalid,
                        $"{path}.value",
                        $"Attribute value is invalid for value type '{requirement.ValueType}'."));
            }

            if (operatorIsValid &&
                valueTypeIsValid &&
                !ManifestAttributeRequirementConversions
                    .IsOperatorSupported(
                        parsedOperator,
                        parsedValueType))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeOperatorValueTypeInvalid,
                        $"{path}.operator",
                        $"Attribute operator '{requirement.Operator}' is not supported for value type '{requirement.ValueType}'."));
            }
        }
        else
        {
            if (requirement.ValueType is not null)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeValueTypeNotAllowed,
                        $"{path}.valueType",
                        $"Attribute operator '{requirement.Operator}' does not accept a value type."));
            }

            if (requirement.HasValue)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeValueNotAllowed,
                        $"{path}.value",
                        $"Attribute operator '{requirement.Operator}' does not accept a value."));
            }
        }

        if (requirement.StringComparison is not null)
        {
            var stringComparisonIsValid =
                ManifestAttributeRequirementConversions
                    .TryParseStringComparison(
                        requirement.StringComparison,
                        out _);

            if (!stringComparisonIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeStringComparisonInvalid,
                        $"{path}.stringComparison",
                        $"Attribute string comparison '{requirement.StringComparison}' is not supported."));
            }
            else if (operatorIsValid &&
                     (!valueTypeIsValid ||
                      !ManifestAttributeRequirementConversions
                          .SupportsStringComparison(
                              parsedOperator,
                              parsedValueType)))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeStringComparisonNotAllowed,
                        $"{path}.stringComparison",
                        $"Attribute string comparison is not supported for operator '{requirement.Operator}' and value type '{requirement.ValueType}'."));
            }
        }
    }

    private static void
        ValidateAttributeComparisonRequirement(
            ManifestAttributeComparisonRequirement requirement,
            string path,
            ICollection<ManifestValidationError> errors)
    {
        var left = requirement.Left is null
            ? OperandValidation.Invalid
            : ValidateAttributeComparisonOperand(
                requirement.Left,
                $"{path}.left",
                errors);

        if (requirement.Left is null)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonLeftRequired,
                    $"{path}.left",
                    "Left attribute-comparison operand is required."));
        }

        var right = requirement.Right is null
            ? OperandValidation.Invalid
            : ValidateAttributeComparisonOperand(
                requirement.Right,
                $"{path}.right",
                errors);

        if (requirement.Right is null)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonRightRequired,
                    $"{path}.right",
                    "Right attribute-comparison operand is required."));
        }

        var operatorIsValid = false;
        var operatorIsBinary = false;
        var parsedOperator = default(
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator);

        if (string.IsNullOrWhiteSpace(
                requirement.Operator))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonOperatorRequired,
                    $"{path}.operator",
                    "Attribute-comparison operator is required."));
        }
        else
        {
            operatorIsValid =
                ManifestAttributeRequirementConversions
                    .TryParseOperator(
                        requirement.Operator,
                        out parsedOperator);

            if (!operatorIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeComparisonOperatorInvalid,
                        $"{path}.operator",
                        $"Attribute-comparison operator '{requirement.Operator}' is not supported."));
            }
            else
            {
                operatorIsBinary =
                    ManifestAttributeRequirementConversions
                        .OperatorRequiresValue(
                            parsedOperator);

                if (!operatorIsBinary)
                {
                    errors.Add(
                        new ManifestValidationError(
                            ManifestValidationCodes
                                .AttributeComparisonOperatorNotBinary,
                            $"{path}.operator",
                            $"Attribute-comparison operator '{requirement.Operator}' does not accept two operands."));
                }
            }
        }

        if (operatorIsValid &&
            operatorIsBinary &&
            !AreComparisonOperandsCompatible(
                parsedOperator,
                left,
                right))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonOperandTypeIncompatible,
                    $"{path}.operator",
                    $"Attribute-comparison operands are not compatible with operator '{requirement.Operator}'."));
        }

        if (requirement.StringComparison is null)
        {
            return;
        }

        var stringComparisonIsValid =
            ManifestAttributeRequirementConversions
                .TryParseStringComparison(
                    requirement.StringComparison,
                    out _);

        if (!stringComparisonIsValid)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonStringComparisonInvalid,
                    $"{path}.stringComparison",
                    $"Attribute-comparison string comparison '{requirement.StringComparison}' is not supported."));

            return;
        }

        if (operatorIsValid &&
            operatorIsBinary &&
            (!ManifestAttributeRequirementConversions
                .OperatorSupportsStringComparison(
                    parsedOperator) ||
             HasKnownNonStringLiteral(left) ||
             HasKnownNonStringLiteral(right)))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonStringComparisonNotAllowed,
                    $"{path}.stringComparison",
                    $"Attribute string comparison is not supported for operator '{requirement.Operator}' and the declared operand types."));
        }
    }

    private static void ValidateTimeWindowRequirement(
        ManifestTimeWindowRequirement requirement,
        string path,
        ICollection<ManifestValidationError> errors)
    {
        if (requirement.Days is null ||
            requirement.Days.Count == 0)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.TimeWindowDaysRequired,
                    $"{path}.days",
                    "At least one time-window day is required."));
        }
        else
        {
            var days = new HashSet<DayOfWeek>();

            for (var index = 0;
                 index < requirement.Days.Count;
                 index++)
            {
                var value = requirement.Days[index];

                if (!ManifestTimeContextConversions.TryParseDay(
                        value,
                        out var day))
                {
                    errors.Add(
                        new ManifestValidationError(
                            ManifestValidationCodes.TimeWindowDayInvalid,
                            $"{path}.days[{index}]",
                            $"Time-window day '{value}' is not supported."));
                }
                else if (!days.Add(day))
                {
                    errors.Add(
                        new ManifestValidationError(
                            ManifestValidationCodes.TimeWindowDayDuplicate,
                            $"{path}.days[{index}]",
                            $"Time-window day '{value}' is duplicated."));
                }
            }
        }

        var startIsValid = false;
        var endIsValid = false;
        var start = default(TimeOnly);
        var end = default(TimeOnly);

        if (string.IsNullOrWhiteSpace(requirement.Start))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.TimeWindowStartRequired,
                    $"{path}.start",
                    "Time-window start is required."));
        }
        else
        {
            startIsValid =
                ManifestTimeContextConversions.TryParseTime(
                    requirement.Start,
                    out start);

            if (!startIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.TimeWindowStartInvalid,
                        $"{path}.start",
                        "Time-window start must use the HH:mm format."));
            }
        }

        if (string.IsNullOrWhiteSpace(requirement.End))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.TimeWindowEndRequired,
                    $"{path}.end",
                    "Time-window end is required."));
        }
        else
        {
            endIsValid =
                ManifestTimeContextConversions.TryParseTime(
                    requirement.End,
                    out end);

            if (!endIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.TimeWindowEndInvalid,
                        $"{path}.end",
                        "Time-window end must use the HH:mm format."));
            }
        }

        if (startIsValid && endIsValid && start == end)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.TimeWindowRangeInvalid,
                    path,
                    "Time-window start and end cannot be equal."));
        }

        if (string.IsNullOrWhiteSpace(requirement.TimeZone))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.TimeWindowTimeZoneRequired,
                    $"{path}.timeZone",
                    "Time-window time zone is required."));
        }
        else if (!ManifestTimeContextConversions.TryParseTimeZone(
                     requirement.TimeZone,
                     out _))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.TimeWindowTimeZoneInvalid,
                    $"{path}.timeZone",
                    $"Time zone '{requirement.TimeZone}' is not available."));
        }
    }

    private static void ValidateDateTimeWindowRequirement(
        ManifestDateTimeWindowRequirement requirement,
        string path,
        ICollection<ManifestValidationError> errors)
    {
        if (requirement.StartsAt is null &&
            requirement.EndsAt is null)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.DateTimeWindowBoundaryRequired,
                    path,
                    "A date-time window must define startsAt, endsAt, or both."));
            return;
        }

        DateTimeOffset? startsAt = null;
        DateTimeOffset? endsAt = null;

        if (requirement.StartsAt is not null)
        {
            if (ManifestTimeContextConversions.TryParseDateTimeOffset(
                    requirement.StartsAt,
                    out var parsed))
            {
                startsAt = parsed;
            }
            else
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.DateTimeWindowStartsAtInvalid,
                        $"{path}.startsAt",
                        "Date-time window startsAt must be an ISO 8601 timestamp with an explicit offset."));
            }
        }

        if (requirement.EndsAt is not null)
        {
            if (ManifestTimeContextConversions.TryParseDateTimeOffset(
                    requirement.EndsAt,
                    out var parsed))
            {
                endsAt = parsed;
            }
            else
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.DateTimeWindowEndsAtInvalid,
                        $"{path}.endsAt",
                        "Date-time window endsAt must be an ISO 8601 timestamp with an explicit offset."));
            }
        }

        if (startsAt is not null &&
            endsAt is not null &&
            startsAt >= endsAt)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.DateTimeWindowRangeInvalid,
                    path,
                    "Date-time window startsAt must be earlier than endsAt."));
        }
    }

    private static void ValidateContextAgeRequirement(
        ManifestContextAgeRequirement requirement,
        string path,
        ICollection<ManifestValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(requirement.Timestamp))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextAgeTimestampRequired,
                    $"{path}.timestamp",
                    "Context-age timestamp is required."));
        }
        else if (!ManifestTimeContextConversions
                     .TryParseContextTimestamp(
                         requirement.Timestamp,
                         out _))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextAgeTimestampInvalid,
                    $"{path}.timestamp",
                    $"Context-age timestamp '{requirement.Timestamp}' is not supported."));
        }

        if (string.IsNullOrWhiteSpace(requirement.MaximumAge))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextAgeMaximumAgeRequired,
                    $"{path}.maximumAge",
                    "Context-age maximumAge is required."));
        }
        else if (!ManifestTimeContextConversions.TryParseMaximumAge(
                     requirement.MaximumAge,
                     out _))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextAgeMaximumAgeInvalid,
                    $"{path}.maximumAge",
                    "Context-age maximumAge must be a positive invariant TimeSpan value."));
        }
    }

    private static void ValidateContextRequirement(
        ManifestContextRequirement requirement,
        string path,
        ICollection<ManifestValidationError> errors)
    {
        var propertyIsValid = false;
        var property = default(
            Fotbiler.RuleGate.Abstractions.Authorization
                .AuthorizationContextProperty);

        if (string.IsNullOrWhiteSpace(requirement.Property))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextPropertyRequired,
                    $"{path}.property",
                    "Context property is required."));
        }
        else
        {
            propertyIsValid =
                ManifestTimeContextConversions.TryParseContextProperty(
                    requirement.Property,
                    out property);

            if (!propertyIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.ContextPropertyInvalid,
                        $"{path}.property",
                        $"Context property '{requirement.Property}' is not supported."));
            }
        }

        var operatorIsValid = false;
        var @operator = default(
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator);

        if (string.IsNullOrWhiteSpace(requirement.Operator))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextOperatorRequired,
                    $"{path}.operator",
                    "Context operator is required."));
        }
        else
        {
            operatorIsValid =
                ManifestAttributeRequirementConversions.TryParseOperator(
                    requirement.Operator,
                    out @operator);

            if (!operatorIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.ContextOperatorInvalid,
                        $"{path}.operator",
                        $"Context operator '{requirement.Operator}' is not supported."));
            }
        }

        var valueTypeIsValid = false;
        var valueType = default(ManifestAttributeValueType);

        if (string.IsNullOrWhiteSpace(requirement.ValueType))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextValueTypeRequired,
                    $"{path}.valueType",
                    "Context value type is required."));
        }
        else
        {
            valueTypeIsValid =
                ManifestAttributeRequirementConversions.TryParseValueType(
                    requirement.ValueType,
                    out valueType);

            if (!valueTypeIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.ContextValueTypeInvalid,
                        $"{path}.valueType",
                        $"Context value type '{requirement.ValueType}' is not supported."));
            }
        }

        if (!requirement.HasValue)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextValueRequired,
                    $"{path}.value",
                    "Context value must be specified."));
        }
        else if (valueTypeIsValid &&
                 !ManifestAttributeRequirementConversions.TryConvertValue(
                     requirement.ValueType,
                     requirement.Value,
                     requirement.HasValue,
                     out _))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextValueInvalid,
                    $"{path}.value",
                    $"Context value is invalid for value type '{requirement.ValueType}'."));
        }

        if (propertyIsValid &&
            operatorIsValid &&
            valueTypeIsValid &&
            !IsContextCombinationSupported(
                property,
                @operator,
                valueType))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes.ContextPropertyOperatorValueInvalid,
                    path,
                    "The context property, operator, and value type are not compatible."));
        }

        if (requirement.StringComparison is not null)
        {
            if (!ManifestAttributeRequirementConversions
                    .TryParseStringComparison(
                        requirement.StringComparison,
                        out _))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.ContextStringComparisonInvalid,
                        $"{path}.stringComparison",
                        $"Context string comparison '{requirement.StringComparison}' is not supported."));
            }
            else if (!propertyIsValid ||
                     property ==
                     Fotbiler.RuleGate.Abstractions.Authorization
                         .AuthorizationContextProperty.TrustedDevice ||
                     !operatorIsValid ||
                     !valueTypeIsValid ||
                     !ManifestAttributeRequirementConversions
                         .SupportsStringComparison(
                             @operator,
                             valueType))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes.ContextStringComparisonNotAllowed,
                        $"{path}.stringComparison",
                        "String comparison is not supported for the declared context property, operator, and value type."));
            }
        }
    }

    private static bool IsContextCombinationSupported(
        Fotbiler.RuleGate.Abstractions.Authorization
            .AuthorizationContextProperty property,
        Fotbiler.RuleGate.Abstractions.Policies
            .AuthorizationAttributeOperator @operator,
        ManifestAttributeValueType valueType)
    {
        if (property ==
            Fotbiler.RuleGate.Abstractions.Authorization
                .AuthorizationContextProperty.TrustedDevice)
        {
            return @operator is
                    Fotbiler.RuleGate.Abstractions.Policies
                        .AuthorizationAttributeOperator.Equal or
                    Fotbiler.RuleGate.Abstractions.Policies
                        .AuthorizationAttributeOperator.NotEqual &&
                valueType == ManifestAttributeValueType.Boolean;
        }

        return @operator switch
        {
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator.Equal or
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator.NotEqual or
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator.Contains or
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator.StartsWith or
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator.EndsWith =>
                valueType == ManifestAttributeValueType.String,

            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator.In or
            Fotbiler.RuleGate.Abstractions.Policies
                .AuthorizationAttributeOperator.NotIn =>
                valueType == ManifestAttributeValueType.StringCollection,

            _ => false
        };
    }

    private static OperandValidation
        ValidateAttributeComparisonOperand(
            ManifestAttributeComparisonOperand operand,
            string path,
            ICollection<ManifestValidationError> errors)
    {
        var hasAttributeMembers =
            operand.Source is not null ||
            operand.Name is not null;

        var hasLiteralMembers =
            operand.ValueType is not null ||
            operand.HasValue;

        if (hasAttributeMembers == hasLiteralMembers)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonOperandKindInvalid,
                    path,
                    "An attribute-comparison operand must define either source and name or valueType and value."));

            return OperandValidation.Invalid;
        }

        if (hasAttributeMembers)
        {
            if (string.IsNullOrWhiteSpace(
                    operand.Source))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeComparisonOperandSourceRequired,
                        $"{path}.source",
                        "Attribute-comparison operand source is required."));
            }
            else if (!ManifestAttributeRequirementConversions
                         .TryParseSource(
                             operand.Source,
                             out _))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeComparisonOperandSourceInvalid,
                        $"{path}.source",
                        $"Attribute-comparison operand source '{operand.Source}' is not supported."));
            }

            if (string.IsNullOrWhiteSpace(
                    operand.Name))
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeComparisonOperandNameRequired,
                        $"{path}.name",
                        "Attribute-comparison operand name is required."));
            }

            return OperandValidation.Attribute;
        }

        var valueTypeIsValid = false;
        var valueType = default(
            ManifestAttributeValueType);

        if (string.IsNullOrWhiteSpace(
                operand.ValueType))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonOperandValueTypeRequired,
                    $"{path}.valueType",
                    "Attribute-comparison literal value type is required."));
        }
        else
        {
            valueTypeIsValid =
                ManifestAttributeRequirementConversions
                    .TryParseValueType(
                        operand.ValueType,
                        out valueType);

            if (!valueTypeIsValid)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .AttributeComparisonOperandValueTypeInvalid,
                        $"{path}.valueType",
                        $"Attribute-comparison literal value type '{operand.ValueType}' is not supported."));
            }
        }

        if (!operand.HasValue)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonOperandValueRequired,
                    $"{path}.value",
                    "Attribute-comparison literal value must be specified."));
        }
        else if (valueTypeIsValid &&
                 !ManifestAttributeRequirementConversions
                     .TryConvertValue(
                         operand.ValueType,
                         operand.Value,
                         operand.HasValue,
                         out _))
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .AttributeComparisonOperandValueInvalid,
                    $"{path}.value",
                    $"Attribute-comparison literal value is invalid for value type '{operand.ValueType}'."));
        }

        return valueTypeIsValid
            ? OperandValidation.Literal(valueType)
            : OperandValidation.Invalid;
    }

    private static bool AreComparisonOperandsCompatible(
        Fotbiler.RuleGate.Abstractions.Policies
            .AuthorizationAttributeOperator @operator,
        OperandValidation left,
        OperandValidation right)
    {
        if (!left.IsValid || !right.IsValid)
        {
            return true;
        }

        if (left.LiteralValueType is { } leftType &&
            !ManifestAttributeRequirementConversions
                .IsLeftOperandTypeSupported(
                    @operator,
                    leftType))
        {
            return false;
        }

        if (right.LiteralValueType is { } rightType &&
            !ManifestAttributeRequirementConversions
                .IsOperatorSupported(
                    @operator,
                    rightType))
        {
            return false;
        }

        return left.LiteralValueType is not { } knownLeft ||
            right.LiteralValueType is not { } knownRight ||
            ManifestAttributeRequirementConversions
                .AreOperandTypesCompatible(
                    @operator,
                    knownLeft,
                    knownRight);
    }

    private static bool HasKnownNonStringLiteral(
        OperandValidation operand)
    {
        return operand.LiteralValueType is { } valueType &&
            !ManifestAttributeRequirementConversions
                .IsStringValueType(valueType);
    }

    private static void ValidateRequirementCollection(
        IReadOnlyList<ManifestRequirement?> requirements,
        string path,
        ICollection<ManifestValidationError> errors)
    {
        if (requirements.Count == 0)
        {
            errors.Add(
                new ManifestValidationError(
                    ManifestValidationCodes
                        .RequirementChildrenRequired,
                    path,
                    "A logical requirement must contain at least one child requirement."));

            return;
        }

        for (var index = 0;
             index < requirements.Count;
             index++)
        {
            var childPath = $"{path}[{index}]";
            var child = requirements[index];

            if (child is null)
            {
                errors.Add(
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .RequirementRequired,
                        childPath,
                        "Child requirement is required."));

                continue;
            }

            ValidateRequirement(
                child,
                childPath,
                errors);
        }
    }

    private readonly record struct OperandValidation(
        bool IsValid,
        ManifestAttributeValueType? LiteralValueType)
    {
        internal static OperandValidation Invalid { get; } =
            new(false, null);

        internal static OperandValidation Attribute { get; } =
            new(true, null);

        internal static OperandValidation Literal(
            ManifestAttributeValueType valueType)
        {
            return new OperandValidation(
                true,
                valueType);
        }
    }

    private readonly record struct PolicyRoute(
        string ResourceType,
        string Action);
}
