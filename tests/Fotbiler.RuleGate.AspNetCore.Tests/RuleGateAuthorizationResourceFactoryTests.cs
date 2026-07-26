using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationResourceFactoryTests
{
    [Fact]
    public void Create_RejectsMissingResource()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        Assert.Throws<InvalidOperationException>(
            () => factory.Create(null));
    }

    [Fact]
    public void Create_RejectsUnsupportedResource()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        Assert.Throws<InvalidOperationException>(
            () => factory.Create(new object()));
    }

    [Fact]
    public void Create_ReturnsRuleGateResource()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var expected =
            new AuthorizationResource(
                type: "document",
                id: "document-1");

        var actual =
            factory.Create(expected);

        Assert.Same(expected, actual);
    }
}
