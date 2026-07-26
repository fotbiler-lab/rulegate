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

    public const string RequirementIdInvalid =
        "MANIFEST_REQUIREMENT_ID_INVALID";

    public const string RequirementKindInvalid =
        "MANIFEST_REQUIREMENT_KIND_INVALID";

    public const string PermissionRequired =
        "MANIFEST_PERMISSION_REQUIRED";

    public const string RoleRequired =
        "MANIFEST_ROLE_REQUIRED";

    public const string RequirementChildrenRequired =
        "MANIFEST_REQUIREMENT_CHILDREN_REQUIRED";

    public const string RequirementRequired =
        "MANIFEST_REQUIREMENT_REQUIRED";
}
