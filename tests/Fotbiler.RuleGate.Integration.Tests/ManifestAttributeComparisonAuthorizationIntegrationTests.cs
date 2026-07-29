using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Integration.Tests;

public sealed class
    ManifestAttributeComparisonAuthorizationIntegrationTests
{
    private const string Manifest = """
        schemaVersion: 1

        application:
          id: ownership-integration
          name: Ownership Integration

        policies:
          - id: document-update
            resourceType: document
            action: update
            requirement:
              all:
                - permission: document.update
                - id: resource-owner
                  attributeComparison:
                    left:
                      source: resource
                      name: ownerId
                    operator: equal
                    right:
                      source: subject
                      name: id

          - id: organization-read
            resourceType: document
            action: organization-read
            requirement:
              id: organization-scope
              attributeComparison:
                left:
                  source: subject
                  name: organizationScopes
                operator: intersects
                right:
                  source: resource
                  name: organizationScopes
                stringComparison: ordinalIgnoreCase

          - id: clearance-read
            resourceType: document
            action: clearance-read
            requirement:
              id: clearance-limit
              attributeComparison:
                left:
                  source: subject
                  name: clearance
                operator: greaterThanOrEqual
                right:
                  valueType: number
                  value: 3
        """;

    [Fact]
    public async Task Allows_matching_resource_owner()
    {
        var decision = await CreateEngine()
            .EvaluateAsync(
                Request(
                    "update",
                    permissions: ["document.update"],
                    subjectAttributes:
                        Attributes(("id", "user-1")),
                    resourceAttributes:
                        Attributes(("ownerId", "user-1"))));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Denies_different_resource_owner()
    {
        var decision = await CreateEngine()
            .EvaluateAsync(
                Request(
                    "update",
                    permissions: ["document.update"],
                    subjectAttributes:
                        Attributes(("id", "user-1")),
                    resourceAttributes:
                        Attributes(("ownerId", "user-2"))));

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeComparisonNotSatisfied,
            Assert.Single(decision.Failures).Code);
    }

    [Fact]
    public async Task Allows_intersecting_organization_scopes()
    {
        var decision = await CreateEngine()
            .EvaluateAsync(
                Request(
                    "organization-read",
                    subjectAttributes:
                        Attributes(
                            ("organizationScopes",
                             new[] { "FINANCE", "legal" })),
                    resourceAttributes:
                        Attributes(
                            ("organizationScopes",
                             new[] { "finance", "sales" }))));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Allows_attribute_to_literal_ordering()
    {
        var decision = await CreateEngine()
            .EvaluateAsync(
                Request(
                    "clearance-read",
                    subjectAttributes:
                        Attributes(("clearance", 4))));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Missing_operand_fails_closed()
    {
        var decision = await CreateEngine()
            .EvaluateAsync(
                Request(
                    "update",
                    permissions: ["document.update"],
                    subjectAttributes:
                        Attributes(("id", "user-1"))));

        Assert.False(decision.IsAllowed);
        Assert.Equal(
            AuthorizationFailureCodes.AttributeNotFound,
            Assert.Single(decision.Failures).Code);
    }

    private static PolicyAuthorizationEngine CreateEngine()
    {
        var compilation =
            new RuleGateManifestCompiler()
                .CompileFromText(Manifest);

        Assert.True(compilation.IsSuccess);
        Assert.Empty(compilation.LoadErrors);
        Assert.Empty(compilation.ValidationErrors);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
                compilation.Policies),
            new RequirementEvaluationDispatcher(
            [
                new PermissionRequirementEvaluator(),
                new AttributeComparisonRequirementEvaluator(),
                new AllRequirementEvaluator()
            ]));
    }

    private static AuthorizationRequest Request(
        string action,
        IEnumerable<string>? permissions = null,
        AuthorizationAttributes? subjectAttributes = null,
        AuthorizationAttributes? resourceAttributes = null)
    {
        return new AuthorizationRequest(
            new AuthorizationSubject(
                "user-1",
                permissions: permissions,
                attributes: subjectAttributes),
            new AuthorizationResource(
                "document",
                "document-1",
                resourceAttributes),
            action,
            new AuthorizationContext(
                DateTimeOffset.UnixEpoch));
    }

    private static AuthorizationAttributes Attributes(
        params (string Name, object? Value)[] values)
    {
        return new AuthorizationAttributes(
            values.Select(
                static value =>
                    new KeyValuePair<string, object?>(
                        value.Name,
                        value.Value)));
    }
}
