namespace Fotbiler.RuleGate.Manifest.Validation;

public static class ManifestValidationCodes
{
    public const string UnsupportedSchemaVersion =
        "MANIFEST_UNSUPPORTED_SCHEMA_VERSION";

    public const string ApplicationRequired =
        "MANIFEST_APPLICATION_REQUIRED";

    public const string ApplicationIdRequired =
        "MANIFEST_APPLICATION_ID_REQUIRED";

    public const string ApplicationNameRequired =
        "MANIFEST_APPLICATION_NAME_REQUIRED";

    public const string PoliciesRequired =
        "MANIFEST_POLICIES_REQUIRED";

    public const string PolicyRequired =
        "MANIFEST_POLICY_REQUIRED";

    public const string PolicyIdRequired =
        "MANIFEST_POLICY_ID_REQUIRED";

    public const string PolicyResourceTypeRequired =
        "MANIFEST_POLICY_RESOURCE_TYPE_REQUIRED";

    public const string PolicyActionRequired =
        "MANIFEST_POLICY_ACTION_REQUIRED";

    public const string PolicyRequirementRequired =
        "MANIFEST_POLICY_REQUIREMENT_REQUIRED";

    public const string DuplicatePolicyId =
        "MANIFEST_DUPLICATE_POLICY_ID";

    public const string DuplicatePolicyRoute =
        "MANIFEST_DUPLICATE_POLICY_ROUTE";

    public const string PolicyCountExceeded =
        "MANIFEST_POLICY_COUNT_EXCEEDED";

    public const string RequirementDepthExceeded =
        "MANIFEST_REQUIREMENT_DEPTH_EXCEEDED";

    public const string RequirementNodeCountExceeded =
        "MANIFEST_REQUIREMENT_NODE_COUNT_EXCEEDED";

    public const string RequirementChildCountExceeded =
        "MANIFEST_REQUIREMENT_CHILD_COUNT_EXCEEDED";

    public const string TotalRequirementNodeCountExceeded =
        "MANIFEST_TOTAL_REQUIREMENT_NODE_COUNT_EXCEEDED";

    public const string RequirementCycleDetected =
        "MANIFEST_REQUIREMENT_CYCLE_DETECTED";

    public const string RequirementIdInvalid =
        "MANIFEST_REQUIREMENT_ID_INVALID";

    public const string RequirementKindInvalid =
        "MANIFEST_REQUIREMENT_KIND_INVALID";

    public const string PermissionRequired =
        "MANIFEST_PERMISSION_REQUIRED";

    public const string RoleRequired =
        "MANIFEST_ROLE_REQUIRED";

    public const string AttributeSourceRequired =
        "MANIFEST_ATTRIBUTE_SOURCE_REQUIRED";

    public const string AttributeSourceInvalid =
        "MANIFEST_ATTRIBUTE_SOURCE_INVALID";

    public const string AttributeNameRequired =
        "MANIFEST_ATTRIBUTE_NAME_REQUIRED";

    public const string AttributeOperatorRequired =
        "MANIFEST_ATTRIBUTE_OPERATOR_REQUIRED";

    public const string AttributeOperatorInvalid =
        "MANIFEST_ATTRIBUTE_OPERATOR_INVALID";

    public const string AttributeValueTypeRequired =
        "MANIFEST_ATTRIBUTE_VALUE_TYPE_REQUIRED";

    public const string AttributeValueTypeInvalid =
        "MANIFEST_ATTRIBUTE_VALUE_TYPE_INVALID";

    public const string AttributeValueRequired =
        "MANIFEST_ATTRIBUTE_VALUE_REQUIRED";

    public const string AttributeValueInvalid =
        "MANIFEST_ATTRIBUTE_VALUE_INVALID";

    public const string AttributeOperatorValueTypeInvalid =
        "MANIFEST_ATTRIBUTE_OPERATOR_VALUE_TYPE_INVALID";

    public const string AttributeValueTypeNotAllowed =
        "MANIFEST_ATTRIBUTE_VALUE_TYPE_NOT_ALLOWED";

    public const string AttributeValueNotAllowed =
        "MANIFEST_ATTRIBUTE_VALUE_NOT_ALLOWED";

    public const string AttributeStringComparisonInvalid =
        "MANIFEST_ATTRIBUTE_STRING_COMPARISON_INVALID";

    public const string AttributeStringComparisonNotAllowed =
        "MANIFEST_ATTRIBUTE_STRING_COMPARISON_NOT_ALLOWED";

    public const string AttributeComparisonLeftRequired =
        "MANIFEST_ATTRIBUTE_COMPARISON_LEFT_REQUIRED";

    public const string AttributeComparisonRightRequired =
        "MANIFEST_ATTRIBUTE_COMPARISON_RIGHT_REQUIRED";

    public const string AttributeComparisonOperatorRequired =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERATOR_REQUIRED";

    public const string AttributeComparisonOperatorInvalid =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERATOR_INVALID";

    public const string AttributeComparisonOperatorNotBinary =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERATOR_NOT_BINARY";

    public const string AttributeComparisonOperandKindInvalid =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_KIND_INVALID";

    public const string AttributeComparisonOperandSourceRequired =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_SOURCE_REQUIRED";

    public const string AttributeComparisonOperandSourceInvalid =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_SOURCE_INVALID";

    public const string AttributeComparisonOperandNameRequired =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_NAME_REQUIRED";

    public const string AttributeComparisonOperandValueTypeRequired =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_VALUE_TYPE_REQUIRED";

    public const string AttributeComparisonOperandValueTypeInvalid =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_VALUE_TYPE_INVALID";

    public const string AttributeComparisonOperandValueRequired =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_VALUE_REQUIRED";

    public const string AttributeComparisonOperandValueInvalid =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_VALUE_INVALID";

    public const string AttributeComparisonOperandTypeIncompatible =
        "MANIFEST_ATTRIBUTE_COMPARISON_OPERAND_TYPE_INCOMPATIBLE";

    public const string AttributeComparisonStringComparisonInvalid =
        "MANIFEST_ATTRIBUTE_COMPARISON_STRING_COMPARISON_INVALID";

    public const string AttributeComparisonStringComparisonNotAllowed =
        "MANIFEST_ATTRIBUTE_COMPARISON_STRING_COMPARISON_NOT_ALLOWED";

    public const string TimeWindowDaysRequired =
        "MANIFEST_TIME_WINDOW_DAYS_REQUIRED";

    public const string TimeWindowDayInvalid =
        "MANIFEST_TIME_WINDOW_DAY_INVALID";

    public const string TimeWindowDayDuplicate =
        "MANIFEST_TIME_WINDOW_DAY_DUPLICATE";

    public const string TimeWindowStartRequired =
        "MANIFEST_TIME_WINDOW_START_REQUIRED";

    public const string TimeWindowStartInvalid =
        "MANIFEST_TIME_WINDOW_START_INVALID";

    public const string TimeWindowEndRequired =
        "MANIFEST_TIME_WINDOW_END_REQUIRED";

    public const string TimeWindowEndInvalid =
        "MANIFEST_TIME_WINDOW_END_INVALID";

    public const string TimeWindowRangeInvalid =
        "MANIFEST_TIME_WINDOW_RANGE_INVALID";

    public const string TimeWindowTimeZoneRequired =
        "MANIFEST_TIME_WINDOW_TIME_ZONE_REQUIRED";

    public const string TimeWindowTimeZoneInvalid =
        "MANIFEST_TIME_WINDOW_TIME_ZONE_INVALID";

    public const string DateTimeWindowBoundaryRequired =
        "MANIFEST_DATE_TIME_WINDOW_BOUNDARY_REQUIRED";

    public const string DateTimeWindowStartsAtInvalid =
        "MANIFEST_DATE_TIME_WINDOW_STARTS_AT_INVALID";

    public const string DateTimeWindowEndsAtInvalid =
        "MANIFEST_DATE_TIME_WINDOW_ENDS_AT_INVALID";

    public const string DateTimeWindowRangeInvalid =
        "MANIFEST_DATE_TIME_WINDOW_RANGE_INVALID";

    public const string ContextAgeTimestampRequired =
        "MANIFEST_CONTEXT_AGE_TIMESTAMP_REQUIRED";

    public const string ContextAgeTimestampInvalid =
        "MANIFEST_CONTEXT_AGE_TIMESTAMP_INVALID";

    public const string ContextAgeMaximumAgeRequired =
        "MANIFEST_CONTEXT_AGE_MAXIMUM_AGE_REQUIRED";

    public const string ContextAgeMaximumAgeInvalid =
        "MANIFEST_CONTEXT_AGE_MAXIMUM_AGE_INVALID";

    public const string ContextPropertyRequired =
        "MANIFEST_CONTEXT_PROPERTY_REQUIRED";

    public const string ContextPropertyInvalid =
        "MANIFEST_CONTEXT_PROPERTY_INVALID";

    public const string ContextOperatorRequired =
        "MANIFEST_CONTEXT_OPERATOR_REQUIRED";

    public const string ContextOperatorInvalid =
        "MANIFEST_CONTEXT_OPERATOR_INVALID";

    public const string ContextValueTypeRequired =
        "MANIFEST_CONTEXT_VALUE_TYPE_REQUIRED";

    public const string ContextValueTypeInvalid =
        "MANIFEST_CONTEXT_VALUE_TYPE_INVALID";

    public const string ContextValueRequired =
        "MANIFEST_CONTEXT_VALUE_REQUIRED";

    public const string ContextValueInvalid =
        "MANIFEST_CONTEXT_VALUE_INVALID";

    public const string ContextPropertyOperatorValueInvalid =
        "MANIFEST_CONTEXT_PROPERTY_OPERATOR_VALUE_INVALID";

    public const string ContextStringComparisonInvalid =
        "MANIFEST_CONTEXT_STRING_COMPARISON_INVALID";

    public const string ContextStringComparisonNotAllowed =
        "MANIFEST_CONTEXT_STRING_COMPARISON_NOT_ALLOWED";

    public const string RequirementChildrenRequired =
        "MANIFEST_REQUIREMENT_CHILDREN_REQUIRED";

    public const string RequirementRequired =
        "MANIFEST_REQUIREMENT_REQUIRED";
}
