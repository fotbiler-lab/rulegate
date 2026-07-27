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
    ManifestAttributeAuthorizationIntegrationTests
{
    private const string Manifest = """
        schemaVersion: 1

        application:
          id: attribute-integration
          name: Attribute Integration

        policies:
          - id: document-read
            resourceType: document
            action: read
            requirement:
              all:
                - permission: document.read
                - id: finance-department
                  attribute:
                    source: subject
                    name: department
                    operator: equal
                    valueType: string
                    value: finance
                - id: classification-limit
                  attribute:
                    source: resource
                    name: classification
                    operator: lessThanOrEqual
                    valueType: number
                    value: 3

          - id: trusted-access
            resourceType: document
            action: access
            requirement:
              id: trusted-network
              attribute:
                source: context
                name: trustedNetwork
                operator: equal
                valueType: boolean
                value: true
        """;

    [Fact]
    public async Task
        EvaluateAsync_allows_matching_manifest_attributes()
    {
        var decision =
            await CreateEngine().EvaluateAsync(
                CreateRequest(
                    action: "read",
                    permissions:
                    [
                        "document.read"
                    ],
                    subjectAttributes:
                        CreateAttributes(
                            "department",
                            "finance"),
                    resourceAttributes:
                        CreateAttributes(
                            "classification",
                            2)));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    [Fact]
    public async Task
        EvaluateAsync_denies_missing_subject_attribute()
    {
        var decision =
            await CreateEngine().EvaluateAsync(
                CreateRequest(
                    action: "read",
                    permissions:
                    [
                        "document.read"
                    ],
                    resourceAttributes:
                        CreateAttributes(
                            "classification",
                            2)));

        Assert.False(decision.IsAllowed);

        var failure =
            Assert.Single(decision.Failures);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeNotFound,
            failure.Code);

        Assert.Equal(
            "finance-department",
            failure.RequirementId);
    }

    [Fact]
    public async Task
        EvaluateAsync_denies_failed_resource_comparison()
    {
        var decision =
            await CreateEngine().EvaluateAsync(
                CreateRequest(
                    action: "read",
                    permissions:
                    [
                        "document.read"
                    ],
                    subjectAttributes:
                        CreateAttributes(
                            "department",
                            "finance"),
                    resourceAttributes:
                        CreateAttributes(
                            "classification",
                            5)));

        Assert.False(decision.IsAllowed);

        var failure =
            Assert.Single(decision.Failures);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeComparisonNotSatisfied,
            failure.Code);

        Assert.Equal(
            "classification-limit",
            failure.RequirementId);
    }

    [Fact]
    public async Task
        EvaluateAsync_denies_attribute_type_mismatch()
    {
        var decision =
            await CreateEngine().EvaluateAsync(
                CreateRequest(
                    action: "read",
                    permissions:
                    [
                        "document.read"
                    ],
                    subjectAttributes:
                        CreateAttributes(
                            "department",
                            "finance"),
                    resourceAttributes:
                        CreateAttributes(
                            "classification",
                            "2")));

        Assert.False(decision.IsAllowed);

        var failure =
            Assert.Single(decision.Failures);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeTypeMismatch,
            failure.Code);

        Assert.Equal(
            "classification-limit",
            failure.RequirementId);
    }

    [Fact]
    public async Task
        EvaluateAsync_allows_matching_context_attribute()
    {
        var decision =
            await CreateEngine().EvaluateAsync(
                CreateRequest(
                    action: "access",
                    contextAttributes:
                        CreateAttributes(
                            "trustedNetwork",
                            true)));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    private static PolicyAuthorizationEngine
        CreateEngine()
    {
        var compilation =
            new RuleGateManifestCompiler()
                .CompileFromText(Manifest);

        Assert.True(compilation.IsSuccess);
        Assert.Empty(compilation.LoadErrors);
        Assert.Empty(
            compilation.ValidationErrors);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
                compilation.Policies),
            new RequirementEvaluationDispatcher(
            [
                new PermissionRequirementEvaluator(),
                new RoleRequirementEvaluator(),
                new AttributeRequirementEvaluator(),
                new AllRequirementEvaluator(),
                new AnyRequirementEvaluator(),
                new NotRequirementEvaluator()
            ]));
    }

    private static AuthorizationRequest CreateRequest(
        string action,
        IEnumerable<string>? permissions = null,
        AuthorizationAttributes? subjectAttributes = null,
        AuthorizationAttributes? resourceAttributes = null,
        AuthorizationAttributes? contextAttributes = null)
    {
        return new AuthorizationRequest(
            subject:
                new AuthorizationSubject(
                    id: "user-1",
                    permissions: permissions,
                    attributes:
                        subjectAttributes),

            resource:
                new AuthorizationResource(
                    type: "document",
                    id: "document-1",
                    attributes:
                        resourceAttributes),

            action,

            context:
                new AuthorizationContext(
                    DateTimeOffset.UnixEpoch,
                    contextAttributes));
    }

    private static AuthorizationAttributes
        CreateAttributes(
            string name,
            object? value)
    {
        return new AuthorizationAttributes(
        [
            new KeyValuePair<string, object?>(
                name,
                value)
        ]);
    }
}
