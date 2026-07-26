using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class AuthorizationPrimitivesTests
{
    [Fact]
    public void Subject_RejectsEmptyIdentifier()
    {
        Assert.Throws<ArgumentException>(
            () => new AuthorizationSubject(""));
    }

    [Fact]
    public void Subject_CopiesRoleCollection()
    {
        var roles = new List<string>
        {
            "role.editor"
        };

        var subject = new AuthorizationSubject(
            id: "user-1",
            roles: roles);

        roles.Add("role.administrator");

        Assert.Contains(
            "role.editor",
            subject.Roles);

        Assert.DoesNotContain(
            "role.administrator",
            subject.Roles);
    }

    [Fact]
    public void Subject_UsesCaseSensitivePermissions()
    {
        var subject = new AuthorizationSubject(
            id: "user-1",
            permissions:
            [
                "resource.read"
            ]);

        Assert.Contains(
            "resource.read",
            subject.Permissions);

        Assert.DoesNotContain(
            "RESOURCE.READ",
            subject.Permissions);
    }

    [Fact]
    public void Attributes_CopySourceValues()
    {
        var source = new Dictionary<string, object?>
        {
            ["department"] = "Legal"
        };

        var attributes =
            new AuthorizationAttributes(source);

        source["department"] = "Finance";

        Assert.Equal(
            "Legal",
            attributes["department"]);
    }

    [Fact]
    public void Resource_AllowsMissingIdentifierForCreate()
    {
        var resource = new AuthorizationResource(
            type: "sample-resource");

        Assert.Null(resource.Id);
    }

    [Fact]
    public void Request_RejectsEmptyAction()
    {
        var subject = new AuthorizationSubject(
            id: "user-1");

        var resource = new AuthorizationResource(
            type: "sample-resource");

        var context = new AuthorizationContext(
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(
            () => new AuthorizationRequest(
                subject,
                resource,
                "",
                context));
    }

    [Fact]
    public void Deny_RequiresAtLeastOneFailure()
    {
        Assert.Throws<ArgumentException>(
            () => AuthorizationDecision.Deny());
    }
}
