using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Mapping;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class RuleGateManifestMapperTests
{
    private readonly RuleGateManifestMapper _mapper =
        new(new RuleGateManifestValidator());

    [Fact]
    public void Map_MapsPermissionRequirement()
    {
        var manifest = CreateManifest(
            new ManifestRequirement
            {
                Permission = "sample.read"
            });

        var result = _mapper.Map(manifest);

        Assert.True(result.IsSuccess);

        var policy = Assert.Single(result.Policies);

        var requirement =
            Assert.IsType<
                PermissionRequirementDefinition>(
                policy.Requirement);

        Assert.Equal(
            "sample.read",
            requirement.Permission);
    }

    [Fact]
    public void Map_MapsRoleRequirement()
    {
        var manifest = CreateManifest(
            new ManifestRequirement
            {
                Role = "sample.editor"
            });

        var result = _mapper.Map(manifest);

        var requirement =
            Assert.IsType<RoleRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);

        Assert.Equal(
            "sample.editor",
            requirement.Role);
    }

    [Fact]
    public void Map_MapsNestedLogicalRequirements()
    {
        var manifest = CreateManifest(
            new ManifestRequirement
            {
                All =
                [
                    new ManifestRequirement
                    {
                        Permission = "sample.read"
                    },
                    new ManifestRequirement
                    {
                        Any =
                        [
                            new ManifestRequirement
                            {
                                Role = "sample.editor"
                            },
                            new ManifestRequirement
                            {
                                Not =
                                    new ManifestRequirement
                                    {
                                        Role =
                                            "sample.blocked"
                                    }
                            }
                        ]
                    }
                ]
            });

        var result = _mapper.Map(manifest);

        var all =
            Assert.IsType<AllRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);

        Assert.Equal(2, all.Requirements.Count);

        var permission =
            Assert.IsType<
                PermissionRequirementDefinition>(
                all.Requirements[0]);

        Assert.Equal(
            "sample.read",
            permission.Permission);

        var any =
            Assert.IsType<AnyRequirementDefinition>(
                all.Requirements[1]);

        Assert.Equal(2, any.Requirements.Count);

        var role =
            Assert.IsType<RoleRequirementDefinition>(
                any.Requirements[0]);

        Assert.Equal(
            "sample.editor",
            role.Role);

        var not =
            Assert.IsType<NotRequirementDefinition>(
                any.Requirements[1]);

        var blockedRole =
            Assert.IsType<RoleRequirementDefinition>(
                not.Requirement);

        Assert.Equal(
            "sample.blocked",
            blockedRole.Role);
    }

    [Fact]
    public void Map_PreservesRequirementIdentifiers()
    {
        var manifest = CreateManifest(
            new ManifestRequirement
            {
                Id = "required-permission",
                Permission = "sample.read"
            });

        var result = _mapper.Map(manifest);

        var requirement =
            Assert.IsType<
                PermissionRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);

        Assert.Equal(
            "required-permission",
            requirement.Id);
    }

    [Fact]
    public void Map_PreservesPolicyIdentityAndCase()
    {
        var manifest = CreateManifest(
            new ManifestRequirement
            {
                Permission = "SAMPLE.READ"
            });

        var policy = manifest.Policies![0]!;

        policy.Id = "SAMPLE-READ";
        policy.ResourceType = "SAMPLE-RESOURCE";
        policy.Action = "READ";

        var mapped =
            Assert.Single(
                _mapper.Map(manifest).Policies);

        Assert.Equal("SAMPLE-READ", mapped.Id);
        Assert.Equal(
            "SAMPLE-RESOURCE",
            mapped.ResourceType);
        Assert.Equal("READ", mapped.Action);

        Assert.Equal(
            "SAMPLE.READ",
            Assert.IsType<
                PermissionRequirementDefinition>(
                mapped.Requirement)
                .Permission);
    }

    [Fact]
    public void Map_ReturnsEmptyPoliciesForValidEmptyCollection()
    {
        var manifest = CreateManifest(
            new ManifestRequirement
            {
                Permission = "sample.read"
            });

        manifest.Policies = [];

        var result = _mapper.Map(manifest);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Map_ReturnsValidationErrorsForInvalidManifest()
    {
        var manifest = CreateManifest(
            new ManifestRequirement());

        var result = _mapper.Map(manifest);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .RequirementKindInvalid);
    }

    [Fact]
    public void Map_RejectsNullManifest()
    {
        Assert.Throws<ArgumentNullException>(
            () => _mapper.Map(null!));
    }

    [Fact]
    public void Constructor_RejectsNullValidator()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RuleGateManifestMapper(
                null!));
    }

    private static RuleGateManifest CreateManifest(
        ManifestRequirement requirement)
    {
        return new RuleGateManifest
        {
            SchemaVersion =
                RuleGateManifestDefaults
                    .SupportedSchemaVersion,
            Application =
                new ManifestApplication
                {
                    Id = "rulegate-example",
                    Name = "RuleGate Example"
                },
            Policies =
            [
                new ManifestPolicy
                {
                    Id = "sample-read",
                    ResourceType =
                        "sample-resource",
                    Action = "read",
                    Requirement = requirement
                }
            ]
        };
    }
}
