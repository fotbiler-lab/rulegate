using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class PolicyInvariantTests
{
    [Fact]
    public void Policy_RejectsEmptyResourceType()
    {
        Assert.Throws<ArgumentException>(
            () => new PolicyDefinition(
                id: "sample-read",
                resourceType: "",
                action: "read",
                requirement: CreateRequirement()));
    }

    [Fact]
    public void Policy_RejectsEmptyAction()
    {
        Assert.Throws<ArgumentException>(
            () => new PolicyDefinition(
                id: "sample-read",
                resourceType: "sample-resource",
                action: "",
                requirement: CreateRequirement()));
    }

    [Fact]
    public void Requirement_RejectsEmptyOptionalIdentifier()
    {
        Assert.Throws<ArgumentException>(
            () => new PermissionRequirementDefinition(
                permission: "sample.read",
                id: ""));
    }

    [Fact]
    public void AllRequirement_RejectsNullChild()
    {
        RequirementDefinition[] requirements =
        [
            null!
        ];

        Assert.Throws<ArgumentException>(
            () => new AllRequirementDefinition(
                requirements));
    }

    [Fact]
    public void AnyRequirement_RejectsNullChild()
    {
        RequirementDefinition[] requirements =
        [
            null!
        ];

        Assert.Throws<ArgumentException>(
            () => new AnyRequirementDefinition(
                requirements));
    }

    [Fact]
    public void AllRequirement_ExposesReadOnlyChildren()
    {
        var requirement = new AllRequirementDefinition(
        [
            CreateRequirement()
        ]);

        var children =
            Assert.IsAssignableFrom<
                IList<RequirementDefinition>>(
                requirement.Requirements);

        Assert.Throws<NotSupportedException>(
            () => children.Add(
                new RoleRequirementDefinition(
                    "sample.editor")));
    }

    [Fact]
    public void AnyRequirement_ExposesReadOnlyChildren()
    {
        var requirement = new AnyRequirementDefinition(
        [
            CreateRequirement()
        ]);

        var children =
            Assert.IsAssignableFrom<
                IList<RequirementDefinition>>(
                requirement.Requirements);

        Assert.Throws<NotSupportedException>(
            () => children.Add(
                new RoleRequirementDefinition(
                    "sample.editor")));
    }

    private static PermissionRequirementDefinition
        CreateRequirement()
    {
        return new PermissionRequirementDefinition(
            "sample.read");
    }
}
