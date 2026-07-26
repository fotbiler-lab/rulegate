using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Models;

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
                    "A requirement must define exactly one of permission, role, all, any, or not."));
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

    private readonly record struct PolicyRoute(
        string ResourceType,
        string Action);
}
