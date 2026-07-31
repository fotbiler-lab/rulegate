using BenchmarkDotNet.Attributes;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Benchmarks;

[MemoryDiagnoser]
public class RequirementBenchmarks
{
    private readonly AuthorizationRequest _request =
        CreateRequest();

    private PolicyAuthorizationEngine _scalar = null!;
    private PolicyAuthorizationEngine _collection = null!;
    private PolicyAuthorizationEngine _comparison = null!;
    private PolicyAuthorizationEngine _logical = null!;
    private PolicyAuthorizationEngine _time = null!;
    private PolicyAuthorizationEngine _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        _scalar = CreateEngine(
            new AttributeRequirementDefinition(
                AuthorizationAttributeSource.Resource,
                "classification",
                AuthorizationAttributeOperator.Equal,
                "internal"));

        _collection = CreateEngine(
            new AttributeRequirementDefinition(
                AuthorizationAttributeSource.Subject,
                "departments",
                AuthorizationAttributeOperator.ContainsAny,
                new[] { "records", "legal" }));

        _comparison = CreateEngine(
            new AttributeComparisonRequirementDefinition(
                AuthorizationAttributeOperand.Subject(
                    "clearance"),
                AuthorizationAttributeOperator
                    .GreaterThanOrEqual,
                AuthorizationAttributeOperand.Resource(
                    "requiredClearance")));

        _logical = CreateEngine(
            new AllRequirementDefinition(
            [
                new PermissionRequirementDefinition(
                    "document.read"),
                new RoleRequirementDefinition(
                    "manager"),
                new AttributeRequirementDefinition(
                    AuthorizationAttributeSource.Resource,
                    "classification",
                    AuthorizationAttributeOperator.Equal,
                    "internal")
            ]));

        _time = CreateEngine(
            new TimeWindowRequirementDefinition(
                Enum.GetValues<DayOfWeek>(),
                TimeSpan.FromHours(8),
                TimeSpan.FromHours(18),
                TimeZoneInfo.Utc));

        _context = CreateEngine(
            new ContextRequirementDefinition(
                AuthorizationContextProperty.OrganizationId,
                AuthorizationAttributeOperator.Equal,
                "records"));
    }

    [Benchmark(Baseline = true)]
    public ValueTask<AuthorizationDecision> ScalarAttribute()
    {
        return _scalar.EvaluateAsync(_request);
    }

    [Benchmark]
    public ValueTask<AuthorizationDecision> CollectionAttribute()
    {
        return _collection.EvaluateAsync(_request);
    }

    [Benchmark]
    public ValueTask<AuthorizationDecision> AttributeToAttribute()
    {
        return _comparison.EvaluateAsync(_request);
    }

    [Benchmark]
    public ValueTask<AuthorizationDecision> LogicalAll()
    {
        return _logical.EvaluateAsync(_request);
    }

    [Benchmark]
    public ValueTask<AuthorizationDecision> TimeWindow()
    {
        return _time.EvaluateAsync(_request);
    }

    [Benchmark]
    public ValueTask<AuthorizationDecision> TrustedContext()
    {
        return _context.EvaluateAsync(_request);
    }

    private static PolicyAuthorizationEngine CreateEngine(
        RequirementDefinition requirement)
    {
        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
            [
                new PolicyDefinition(
                    "benchmark-policy",
                    "document",
                    "read",
                    requirement)
            ]),
            new RequirementEvaluationDispatcher(
            [
                new PermissionRequirementEvaluator(),
                new RoleRequirementEvaluator(),
                new AttributeRequirementEvaluator(),
                new AttributeComparisonRequirementEvaluator(),
                new TimeWindowRequirementEvaluator(),
                new ContextRequirementEvaluator(),
                new AllRequirementEvaluator()
            ]));
    }

    private static AuthorizationRequest CreateRequest()
    {
        var subjectAttributes = new AuthorizationAttributes(
        [
            new("departments", new[] { "records", "finance" }),
            new("clearance", 5L)
        ]);

        var resourceAttributes = new AuthorizationAttributes(
        [
            new("classification", "internal"),
            new("requiredClearance", 3L)
        ]);

        var contextAttributes = new AuthorizationAttributes(
        [
            new(
                AuthorizationContextAttributeNames.OrganizationId,
                "records")
        ]);

        return new AuthorizationRequest(
            new AuthorizationSubject(
                "benchmark-subject",
                roles: ["manager"],
                permissions: ["document.read"],
                attributes: subjectAttributes),
            new AuthorizationResource(
                "document",
                "benchmark-resource",
                resourceAttributes),
            "read",
            new AuthorizationContext(
                new DateTimeOffset(
                    2026,
                    7,
                    27,
                    10,
                    0,
                    0,
                    TimeSpan.Zero),
                contextAttributes));
    }
}
