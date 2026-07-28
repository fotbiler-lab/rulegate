using System.Security.Claims;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.Keycloak.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services
    .AddRuleGate()
    .UseKeycloakSubjectMapping(
        options =>
            options.ClientIds.Add("rulegate-api"));

using var provider =
    services.BuildServiceProvider(
        new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

var factory =
    provider.GetRequiredService<
        IRuleGateSubjectFactory>();

var principal = new ClaimsPrincipal(
    new ClaimsIdentity(
    [
        new Claim("sub", "package-user"),
        new Claim(
            "realm_access",
            """{"roles":["administrator"]}"""),
        new Claim(
            "resource_access",
            """{"rulegate-api":{"roles":["documents.reader"]}}"""),
        new Claim("permission", "documents.read"),
    ],
    authenticationType: "Bearer"));

var subject = factory.Create(principal);

if (subject.Id != "package-user" ||
    !subject.Roles.Contains(
        "keycloak:realm:administrator") ||
    !subject.Roles.Contains(
        "keycloak:client:rulegate-api:documents.reader") ||
    !subject.Permissions.Contains(
        "documents.read"))
{
    throw new InvalidOperationException(
        "The packaged Keycloak integration did not create the expected RuleGate subject.");
}

Console.WriteLine(
    "RuleGate Keycloak package consumer smoke test passed.");
