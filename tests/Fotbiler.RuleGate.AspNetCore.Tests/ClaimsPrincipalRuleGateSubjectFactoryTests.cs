using System.Security.Claims;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Microsoft.Extensions.Options;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    ClaimsPrincipalRuleGateSubjectFactoryTests
{
    [Fact]
    public void Create_RequiresPrincipal()
    {
        var factory = CreateFactory();

        Assert.Throws<ArgumentNullException>(
            () => factory.Create(null!));
    }

    [Fact]
    public void Create_RejectsMissingSubjectIdentifier()
    {
        var factory = CreateFactory();

        var principal = CreatePrincipal(
            new Claim(
                ClaimTypes.Role,
                "sample.editor"));

        Assert.Throws<InvalidOperationException>(
            () => factory.Create(principal));
    }

    [Fact]
    public void Create_RejectsAmbiguousSubjectIdentifiers()
    {
        var factory = CreateFactory();

        var principal = CreatePrincipal(
            new Claim(
                ClaimTypes.NameIdentifier,
                "user-1"),
            new Claim(
                ClaimTypes.NameIdentifier,
                "user-2"));

        Assert.Throws<InvalidOperationException>(
            () => factory.Create(principal));
    }

    [Fact]
    public void Create_MapsDefaultClaimTypes()
    {
        var factory = CreateFactory();

        var principal = CreatePrincipal(
            new Claim(
                ClaimTypes.NameIdentifier,
                "user-1"),
            new Claim(
                ClaimTypes.Role,
                "sample.editor"),
            new Claim(
                RuleGateSubjectOptions
                    .DefaultPermissionClaimType,
                "sample.read"));

        var subject = factory.Create(principal);

        Assert.Equal("user-1", subject.Id);

        Assert.Contains(
            "sample.editor",
            subject.Roles);

        Assert.Contains(
            "sample.read",
            subject.Permissions);
    }

    [Fact]
    public void Create_RemovesExactDuplicateValues()
    {
        var factory = CreateFactory();

        var principal = CreatePrincipal(
            new Claim(
                ClaimTypes.NameIdentifier,
                "user-1"),
            new Claim(
                ClaimTypes.Role,
                "sample.editor"),
            new Claim(
                ClaimTypes.Role,
                "sample.editor"),
            new Claim(
                RuleGateSubjectOptions
                    .DefaultPermissionClaimType,
                "sample.read"),
            new Claim(
                RuleGateSubjectOptions
                    .DefaultPermissionClaimType,
                "sample.read"));

        var subject = factory.Create(principal);

        Assert.Single(subject.Roles);
        Assert.Single(subject.Permissions);
    }

    [Fact]
    public void Create_PreservesCaseSensitiveValues()
    {
        var factory = CreateFactory();

        var principal = CreatePrincipal(
            new Claim(
                ClaimTypes.NameIdentifier,
                "user-1"),
            new Claim(
                ClaimTypes.Role,
                "Editor"),
            new Claim(
                ClaimTypes.Role,
                "editor"),
            new Claim(
                RuleGateSubjectOptions
                    .DefaultPermissionClaimType,
                "Document.Read"),
            new Claim(
                RuleGateSubjectOptions
                    .DefaultPermissionClaimType,
                "document.read"));

        var subject = factory.Create(principal);

        Assert.Equal(2, subject.Roles.Count);
        Assert.Contains("Editor", subject.Roles);
        Assert.Contains("editor", subject.Roles);

        Assert.Equal(2, subject.Permissions.Count);
        Assert.Contains(
            "Document.Read",
            subject.Permissions);

        Assert.Contains(
            "document.read",
            subject.Permissions);
    }

    [Fact]
    public void Create_IgnoresWhitespaceRoleAndPermissionValues()
    {
        var factory = CreateFactory();

        var principal = CreatePrincipal(
            new Claim(
                ClaimTypes.NameIdentifier,
                "user-1"),
            new Claim(
                ClaimTypes.Role,
                " "),
            new Claim(
                RuleGateSubjectOptions
                    .DefaultPermissionClaimType,
                "\t"));

        var subject = factory.Create(principal);

        Assert.Empty(subject.Roles);
        Assert.Empty(subject.Permissions);
    }

    [Fact]
    public void Create_UsesConfiguredClaimTypes()
    {
        var factory = CreateFactory(
            options =>
            {
                options.SubjectIdClaimType =
                    "custom-subject";

                options.RoleClaimTypes.Clear();
                options.RoleClaimTypes.Add(
                    "custom-role");

                options.PermissionClaimTypes.Clear();
                options.PermissionClaimTypes.Add(
                    "custom-permission");
            });

        var principal = CreatePrincipal(
            new Claim(
                ClaimTypes.NameIdentifier,
                "ignored-user"),
            new Claim(
                "custom-subject",
                "configured-user"),
            new Claim(
                ClaimTypes.Role,
                "ignored-role"),
            new Claim(
                "custom-role",
                "configured-role"),
            new Claim(
                RuleGateSubjectOptions
                    .DefaultPermissionClaimType,
                "ignored.permission"),
            new Claim(
                "custom-permission",
                "configured.permission"));

        var subject = factory.Create(principal);

        Assert.Equal(
            "configured-user",
            subject.Id);

        Assert.Single(subject.Roles);

        Assert.Contains(
            "configured-role",
            subject.Roles);

        Assert.Single(subject.Permissions);

        Assert.Contains(
            "configured.permission",
            subject.Permissions);
    }

    [Fact]
    public void Constructor_RejectsBlankSubjectIdClaimType()
    {
        var options =
            new RuleGateSubjectOptions
            {
                SubjectIdClaimType = " ",
            };

        Assert.Throws<ArgumentException>(
            () => new
                ClaimsPrincipalRuleGateSubjectFactory(
                    Options.Create(options)));
    }

    [Fact]
    public void Constructor_RejectsBlankRoleClaimType()
    {
        var options =
            new RuleGateSubjectOptions();

        options.RoleClaimTypes.Add(" ");

        Assert.Throws<ArgumentException>(
            () => new
                ClaimsPrincipalRuleGateSubjectFactory(
                    Options.Create(options)));
    }

    [Fact]
    public void Constructor_RejectsBlankPermissionClaimType()
    {
        var options =
            new RuleGateSubjectOptions();

        options.PermissionClaimTypes.Add(" ");

        Assert.Throws<ArgumentException>(
            () => new
                ClaimsPrincipalRuleGateSubjectFactory(
                    Options.Create(options)));
    }

    private static
        ClaimsPrincipalRuleGateSubjectFactory
        CreateFactory(
            Action<RuleGateSubjectOptions>?
                configure = null)
    {
        var options =
            new RuleGateSubjectOptions();

        configure?.Invoke(options);

        return new
            ClaimsPrincipalRuleGateSubjectFactory(
                Options.Create(options));
    }

    private static ClaimsPrincipal CreatePrincipal(
        params Claim[] claims)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                authenticationType: "Test"));
    }
}
