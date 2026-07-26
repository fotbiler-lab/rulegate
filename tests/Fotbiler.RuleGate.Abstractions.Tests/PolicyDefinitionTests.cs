using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class PolicyDefinitionTests
{
    [Fact]
    public void Policy_StoresGenericDefinition()
    {
        var requirement =
            new PermissionRequirementDefinition(
                permission: "sample.read");

        var policy = new PolicyDefinition(
            id: "sample-resource-read",
            resourceType: "sample-resource",
            action: "read",
            requirement: requirement);

        Assert.Equal(
            "sample-resource-read",
            policy.Id);

        Assert.Equal(
            "sample-resource",
            policy.ResourceType);

        Assert.Equal(
            "read",
            policy.Action);

        Assert.Same(
            requirement,
            policy.Requirement);
    }

    [Fact]
    public void Policy_RejectsEmptyIdentifier()
    {
        var requirement =
            new PermissionRequirementDefinition(
                "sample.read");

        Assert.Throws<ArgumentException>(
            () => new PolicyDefinition(
                id: "",
                resourceType: "sample-resource",
                action: "read",
                requirement: requirement));
    }

    [Fact]
    public void PermissionRequirement_RejectsEmptyPermission()
    {
        Assert.Throws<ArgumentException>(
            () => new PermissionRequirementDefinition(""));
    }

    [Fact]
    public void RoleRequirement_RejectsEmptyRole()
    {
        Assert.Throws<ArgumentException>(
            () => new RoleRequirementDefinition(""));
    }

    [Fact]
    public void AllRequirement_RequiresAtLeastOneChild()
    {
        Assert.Throws<ArgumentException>(
            () => new AllRequirementDefinition([]));
    }

    [Fact]
    public void AnyRequirement_RequiresAtLeastOneChild()
    {
        Assert.Throws<ArgumentException>(
            () => new AnyRequirementDefinition([]));
    }

    [Fact]
    public void AllRequirement_CopiesChildCollection()
    {
        var requirements = new List<RequirementDefinition>
        {
            new PermissionRequirementDefinition(
                "sample.read")
        };

        var all =
            new AllRequirementDefinition(requirements);

        requirements.Add(
            new RoleRequirementDefinition(
                "sample.editor"));

        Assert.Single(all.Requirements);
    }

    [Fact]
    public void NotRequirement_StoresChildRequirement()
    {
        var child =
            new RoleRequirementDefinition(
                "sample.blocked");

        var not =
            new NotRequirementDefinition(child);

        Assert.Same(
            child,
            not.Requirement);
    }

    [Fact]
    public void Requirement_PreservesOptionalIdentifier()
    {
        var requirement =
            new PermissionRequirementDefinition(
                permission: "sample.read",
                id: "required-permission");

        Assert.Equal(
            "required-permission",
            requirement.Id);
    }
}
