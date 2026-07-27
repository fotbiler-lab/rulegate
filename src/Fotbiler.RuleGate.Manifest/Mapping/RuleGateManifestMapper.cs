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

            ManifestAttributeRequirementConversions
                .TryConvertValue(
                    attribute,
                    out var value);

            return new AttributeRequirementDefinition(
                source,
                attribute.Name!,
                @operator,
                value,
                requirement.Id);
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
}
