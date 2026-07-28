using System.Security.Claims;
using Fotbiler.RuleGate.Keycloak.Subjects;
using Microsoft.Extensions.Options;

namespace Fotbiler.RuleGate.Keycloak.Tests;

public sealed class KeycloakRuleGateSubjectFactoryTests
{
    [Fact]
    public void Create_MapsRealmAndSelectedClientRoles()
    {
        var factory = CreateFactory(
            options =>
            {
                options.ClientIds.Add("web portal");
            });

        var subject = factory.Create(
            CreatePrincipal(
                new Claim("sub", "user-1"),
                new Claim(
                    "realm_access",
                    """{"roles":["admin","composite-child","admin"]}"""),
                new Claim(
                    "resource_access",
                    """{"web portal":{"roles":["reader"]},"ignored":{"roles":["owner"]}}"""),
                new Claim("permission", "documents.read"),
                new Claim(
                    "permission",
                    """["documents.write","documents.read"]""")));

        Assert.Equal("user-1", subject.Id);
        Assert.Equal(3, subject.Roles.Count);
        Assert.Contains(
            "keycloak:realm:admin",
            subject.Roles);
        Assert.Contains(
            "keycloak:realm:composite-child",
            subject.Roles);
        Assert.Contains(
            "keycloak:client:web%20portal:reader",
            subject.Roles);
        Assert.DoesNotContain(
            "keycloak:client:ignored:owner",
            subject.Roles);
        Assert.Equal(2, subject.Permissions.Count);
    }

    [Fact]
    public void Create_RequiresAnAuthenticatedPrincipalByDefault()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim("sub", "user-1")]));

        Assert.Throws<InvalidOperationException>(
            () => CreateFactory().Create(principal));
    }

    [Fact]
    public void Create_RejectsMalformedRealmAccess()
    {
        var principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim(
                "realm_access",
                """{"roles":"admin"}"""));

        Assert.Throws<InvalidOperationException>(
            () => CreateFactory().Create(principal));
    }

    [Fact]
    public void Create_IgnoresUnselectedResourceAccess()
    {
        var principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim(
                "resource_access",
                "not-json"));

        var subject = CreateFactory()
            .Create(principal);

        Assert.Empty(subject.Roles);
    }

    [Fact]
    public void Create_RejectsMalformedSelectedClientAccess()
    {
        var factory = CreateFactory(
            options =>
                options.ClientIds.Add("web"));

        var principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim(
                "resource_access",
                """{"web":{"roles":[1]}}"""));

        Assert.Throws<InvalidOperationException>(
            () => factory.Create(principal));
    }

    [Fact]
    public void Create_RejectsAmbiguousSubjectAndStructuredClaims()
    {
        var ambiguousSubject = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim("sub", "user-2"));

        Assert.Throws<InvalidOperationException>(
            () => CreateFactory()
                .Create(ambiguousSubject));

        var ambiguousRealm = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim(
                "realm_access",
                """{"roles":["admin"]}"""),
            new Claim(
                "realm_access",
                """{"roles":["reader"]}"""));

        Assert.Throws<InvalidOperationException>(
            () => CreateFactory()
                .Create(ambiguousRealm));
    }

    [Fact]
    public void Create_CanDisableRealmRoleMapping()
    {
        var factory = CreateFactory(
            options =>
                options.IncludeRealmRoles = false);

        var subject = factory.Create(
            CreatePrincipal(
                new Claim("sub", "user-1"),
                new Claim(
                    "realm_access",
                    "not-json")));

        Assert.Empty(subject.Roles);
    }

    private static KeycloakRuleGateSubjectFactory
        CreateFactory(
            Action<RuleGateKeycloakSubjectOptions>?
                configure = null)
    {
        var options =
            new RuleGateKeycloakSubjectOptions();

        configure?.Invoke(options);

        return new KeycloakRuleGateSubjectFactory(
            Options.Create(options));
    }

    private static ClaimsPrincipal CreatePrincipal(
        params Claim[] claims)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                "Bearer"));
    }
}
