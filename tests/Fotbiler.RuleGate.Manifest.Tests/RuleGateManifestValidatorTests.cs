using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class RuleGateManifestValidatorTests
{
    private readonly RuleGateManifestValidator _validator =
        new();

    [Fact]
    public void Validate_AcceptsValidNestedManifest()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
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
            };

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsUnsupportedSchemaVersion()
    {
        var manifest = CreateValidManifest();

        manifest.SchemaVersion = 999;

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes
                .UnsupportedSchemaVersion,
            error.Code);

        Assert.Equal("schemaVersion", error.Path);
    }

    [Fact]
    public void Validate_RejectsMissingApplication()
    {
        var manifest = CreateValidManifest();

        manifest.Application = null;

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .ApplicationRequired);
    }

    [Fact]
    public void Validate_RejectsMissingApplicationFields()
    {
        var manifest = CreateValidManifest();

        manifest.Application =
            new ManifestApplication();

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .ApplicationIdRequired);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .ApplicationNameRequired);
    }

    [Fact]
    public void Validate_RejectsMissingPoliciesCollection()
    {
        var manifest = CreateValidManifest();

        manifest.Policies = null;

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes.PoliciesRequired,
            error.Code);
    }

    [Fact]
    public void Validate_AllowsEmptyPoliciesCollection()
    {
        var manifest = CreateValidManifest();

        manifest.Policies = [];

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsNullPolicy()
    {
        var manifest = CreateValidManifest();

        manifest.Policies =
        [
            null
        ];

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes.PolicyRequired,
            error.Code);

        Assert.Equal("policies[0]", error.Path);
    }

    [Fact]
    public void Validate_RejectsDuplicatePolicyIdentifiers()
    {
        var manifest = CreateValidManifest();

        manifest.Policies!.Add(
            CreatePolicy(
                id: "sample-read",
                resourceType: "another-resource",
                action: "write"));

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .DuplicatePolicyId);
    }

    [Fact]
    public void Validate_RejectsDuplicatePolicyRoutes()
    {
        var manifest = CreateValidManifest();

        manifest.Policies!.Add(
            CreatePolicy(
                id: "another-policy",
                resourceType: "sample-resource",
                action: "read"));

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .DuplicatePolicyRoute);
    }

    [Fact]
    public void Validate_UsesCaseSensitivePolicyIdentity()
    {
        var manifest = CreateValidManifest();

        manifest.Policies!.Add(
            CreatePolicy(
                id: "SAMPLE-READ",
                resourceType: "SAMPLE-RESOURCE",
                action: "READ"));

        var result = _validator.Validate(manifest);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsPolicyWithoutRequirement()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement = null;

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .PolicyRequirementRequired);
    }

    [Fact]
    public void Validate_RejectsRequirementWithoutKind()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            new ManifestRequirement();

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .RequirementKindInvalid);
    }

    [Fact]
    public void Validate_RejectsRequirementWithMultipleKinds()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            new ManifestRequirement
            {
                Permission = "sample.read",
                Role = "sample.editor"
            };

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .RequirementKindInvalid);
    }

    [Fact]
    public void Validate_RejectsBlankPermission()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            new ManifestRequirement
            {
                Permission = ""
            };

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .PermissionRequired);
    }

    [Fact]
    public void Validate_RejectsEmptyLogicalRequirement()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            new ManifestRequirement
            {
                All = []
            };

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .RequirementChildrenRequired);
    }

    [Fact]
    public void Validate_ReportsNestedNullRequirementPath()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            new ManifestRequirement
            {
                Any =
                [
                    null
                ]
            };

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes
                .RequirementRequired,
            error.Code);

        Assert.Equal(
            "policies[0].requirement.any[0]",
            error.Path);
    }

    [Fact]
    public void Validate_RejectsBlankOptionalRequirementId()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            new ManifestRequirement
            {
                Id = "",
                Permission = "sample.read"
            };

        var result = _validator.Validate(manifest);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                ManifestValidationCodes
                    .RequirementIdInvalid);
    }

    [Fact]
    public void Validate_RejectsNullManifest()
    {
        Assert.Throws<ArgumentNullException>(
            () => _validator.Validate(null!));
    }

    [Fact]
    public void Validate_RejectsPolicyCountAboveLimit()
    {
        var manifest = CreateValidManifest();

        manifest.Policies =
            Enumerable.Range(
                    0,
                    4_097)
                .Select(
                    static index =>
                        (ManifestPolicy?)
                            CreateResourceLimitPolicy(
                                index,
                                CreatePermissionRequirement(
                                    index)))
                .ToList();

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes
                .PolicyCountExceeded,
            error.Code);

        Assert.Equal(
            "policies",
            error.Path);
    }

    [Fact]
    public void Validate_AllowsRequirementAtMaximumDepth()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            CreateNotChain(
                nodeCount: 64);

        var result =
            _validator.Validate(manifest);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsRequirementAboveMaximumDepth()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            CreateNotChain(
                nodeCount: 65);

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes
                .RequirementDepthExceeded,
            error.Code);
    }

    [Fact]
    public void Validate_AllowsMaximumCompositeChildCount()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            new ManifestRequirement
            {
                All =
                    CreatePermissionRequirements(
                        count: 1_024)
            };

        var result =
            _validator.Validate(manifest);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsCompositeAboveMaximumChildCount()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            new ManifestRequirement
            {
                All =
                    CreatePermissionRequirements(
                        count: 1_025)
            };

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes
                .RequirementChildCountExceeded,
            error.Code);

        Assert.Equal(
            "policies[0].requirement.all",
            error.Path);
    }

    [Fact]
    public void Validate_RejectsPolicyRequirementAboveNodeLimit()
    {
        var manifest = CreateValidManifest();

        manifest.Policies![0]!.Requirement =
            CreateRequirementAbovePolicyNodeLimit();

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes
                .RequirementNodeCountExceeded,
            error.Code);
    }

    [Fact]
    public void Validate_RejectsManifestAboveTotalNodeLimit()
    {
        var manifest = CreateValidManifest();

        manifest.Policies =
            Enumerable.Range(
                    0,
                    32)
                .Select(
                    static index =>
                        (ManifestPolicy?)
                            CreateResourceLimitPolicy(
                                index,
                                CreateTwoLevelRequirementTree()))
                .ToList();

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes
                .TotalRequirementNodeCountExceeded,
            error.Code);

        Assert.Equal(
            "policies",
            error.Path);
    }

    [Fact]
    public void Validate_RejectsRequirementCycle()
    {
        var manifest = CreateValidManifest();

        var requirement =
            new ManifestRequirement();

        requirement.Not = requirement;

        manifest.Policies![0]!.Requirement =
            requirement;

        var error = Assert.Single(
            _validator.Validate(manifest).Errors);

        Assert.Equal(
            ManifestValidationCodes
                .RequirementCycleDetected,
            error.Code);
    }

    private static ManifestRequirement
        CreateNotChain(
            int nodeCount)
    {
        if (nodeCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeCount));
        }

        ManifestRequirement current =
            new()
            {
                Permission = "sample.read"
            };

        for (var index = 1;
             index < nodeCount;
             index++)
        {
            current =
                new ManifestRequirement
                {
                    Not = current
                };
        }

        return current;
    }

    private static List<ManifestRequirement?>
        CreatePermissionRequirements(
            int count)
    {
        return Enumerable.Range(
                0,
                count)
            .Select(
                static index =>
                    (ManifestRequirement?)
                        CreatePermissionRequirement(
                            index))
            .ToList();
    }

    private static ManifestRequirement
        CreateRequirementAbovePolicyNodeLimit()
    {
        return new ManifestRequirement
        {
            All =
                Enumerable.Range(
                        0,
                        1_024)
                    .Select(
                        static groupIndex =>
                            (ManifestRequirement?)
                                new ManifestRequirement
                                {
                                    All =
                                        Enumerable.Range(
                                                0,
                                                4)
                                            .Select(
                                                childIndex =>
                                                    (ManifestRequirement?)
                                                        CreatePermissionRequirement(
                                                            groupIndex *
                                                                4 +
                                                            childIndex))
                                            .ToList()
                                })
                    .ToList()
        };
    }

    private static ManifestRequirement
        CreateTwoLevelRequirementTree()
    {
        return new ManifestRequirement
        {
            All =
                Enumerable.Range(
                        0,
                        1_024)
                    .Select(
                        static index =>
                            (ManifestRequirement?)
                                new ManifestRequirement
                                {
                                    Not =
                                        CreatePermissionRequirement(
                                            index)
                                })
                    .ToList()
        };
    }

    private static ManifestRequirement
        CreatePermissionRequirement(
            int index)
    {
        return new ManifestRequirement
        {
            Permission =
                $"sample.permission.{index}"
        };
    }

    private static ManifestPolicy
        CreateResourceLimitPolicy(
            int index,
            ManifestRequirement requirement)
    {
        return new ManifestPolicy
        {
            Id = $"resource-limit-{index}",
            ResourceType =
                $"resource-limit-{index}",
            Action = "read",
            Requirement = requirement
        };
    }

    private static RuleGateManifest CreateValidManifest()
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
                CreatePolicy(
                    id: "sample-read",
                    resourceType: "sample-resource",
                    action: "read")
            ]
        };
    }

    private static ManifestPolicy CreatePolicy(
        string id,
        string resourceType,
        string action)
    {
        return new ManifestPolicy
        {
            Id = id,
            ResourceType = resourceType,
            Action = action,
            Requirement =
                new ManifestRequirement
                {
                    Permission = "sample.read"
                }
        };
    }
}
