using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Integration.Tests;

public sealed class DocumentApprovalPolicyTests
{
    private static readonly DateTimeOffset BusinessHours =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset OutsideBusinessHours =
        new(2026, 7, 29, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Read_allows_public_document_outside_business_hours()
    {
        var decision = await EvaluateAsync(
            action: "read",
            permissions: ["DOC.READ"],
            subject: Subject("records", clearanceLevel: 1),
            resource: Resource("records", classificationLevel: 1),
            evaluationTime: OutsideBusinessHours);

        Assert.True(decision.IsAllowed);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task List_requires_read_permission(
        bool hasPermission,
        bool expected)
    {
        var decision = await EvaluateAsync(
            action: "list",
            permissions: hasPermission ? ["DOC.READ"] : [],
            evaluationTime: BusinessHours);

        Assert.Equal(expected, decision.IsAllowed);
    }

    [Fact]
    public async Task Read_allows_confidential_document_during_business_hours()
    {
        var decision = await EvaluateAsync(
            action: "read",
            permissions: ["DOC.READ"],
            subject: Subject("records", clearanceLevel: 3),
            resource: Resource("records", classificationLevel: 3),
            context: BusinessHoursContext(isOpen: true),
            evaluationTime: BusinessHours);

        Assert.True(decision.IsAllowed);
    }

    [Theory]
    [InlineData(1, 59, false)]
    [InlineData(2, 0, true)]
    [InlineData(18, 59, true)]
    [InlineData(19, 0, false)]
    public async Task Confidential_read_uses_half_open_global_time_envelope(
        int utcHour,
        int utcMinute,
        bool expected)
    {
        var evaluationTime = new DateTimeOffset(
            2026,
            7,
            30,
            utcHour,
            utcMinute,
            0,
            TimeSpan.Zero);
        var decision = await EvaluateAsync(
            action: "read",
            permissions: ["DOC.READ"],
            subject: Subject("records", clearanceLevel: 3),
            resource: Resource("records", classificationLevel: 3),
            context: BusinessHoursContext(isOpen: true),
            evaluationTime: evaluationTime);

        Assert.Equal(expected, decision.IsAllowed);
    }

    [Fact]
    public async Task Confidential_read_denies_when_organization_business_hours_are_closed()
    {
        var decision = await EvaluateAsync(
            action: "read",
            permissions: ["DOC.READ"],
            subject: Subject("records", clearanceLevel: 3),
            resource: Resource("records", classificationLevel: 3),
            context: BusinessHoursContext(isOpen: false),
            evaluationTime: BusinessHours);

        Assert.False(decision.IsAllowed);
    }

    [Theory]
    [InlineData(false, "records", 3, "records", 1, false)]
    [InlineData(true, "legal", 3, "records", 1, false)]
    [InlineData(true, "records", 1, "records", 2, false)]
    [InlineData(true, "records", 3, "records", 3, true)]
    public async Task Read_denies_when_a_resource_control_fails(
        bool hasPermission,
        string subjectOrganization,
        long clearanceLevel,
        string resourceOrganization,
        long classificationLevel,
        bool outsideBusinessHours)
    {
        var decision = await EvaluateAsync(
            action: "read",
            permissions: hasPermission ? ["DOC.READ"] : [],
            subject: Subject(subjectOrganization, clearanceLevel),
            resource: Resource(resourceOrganization, classificationLevel),
            context: BusinessHoursContext(isOpen: !outsideBusinessHours),
            evaluationTime: outsideBusinessHours
                ? OutsideBusinessHours
                : BusinessHours);

        Assert.False(decision.IsAllowed);
        Assert.NotEmpty(decision.Failures);
    }

    [Theory]
    [InlineData("api", true)]
    [InlineData("background", false)]
    [InlineData(null, false)]
    public async Task Create_requires_permission_and_trusted_api_channel(
        string? requestChannel,
        bool expected)
    {
        var context = requestChannel is null
            ? AuthorizationAttributes.Empty
            : Attributes((AuthorizationContextAttributeNames.RequestChannel, requestChannel));
        var decision = await EvaluateAsync(
            action: "create",
            permissions: ["DOC.CREATE"],
            context: context,
            evaluationTime: BusinessHours);

        Assert.Equal(expected, decision.IsAllowed);
    }

    [Fact]
    public async Task Create_denies_without_create_permission()
    {
        var decision = await EvaluateAsync(
            action: "create",
            context: Attributes(
                (AuthorizationContextAttributeNames.RequestChannel, "api")),
            evaluationTime: BusinessHours);

        Assert.False(decision.IsAllowed);
    }

    [Theory]
    [InlineData("DOC.CREATE", 2, 2, true)]
    [InlineData("DOC.UPDATE", 3, 3, true)]
    [InlineData("DOC.CREATE", 2, 3, false)]
    [InlineData("DOC.READ", 3, 1, false)]
    public async Task Classification_respects_permission_and_clearance(
        string permission,
        long clearanceLevel,
        long classificationLevel,
        bool expected)
    {
        var decision = await EvaluateAsync(
            action: "classify",
            permissions: [permission],
            subject: Subject("records", clearanceLevel),
            resource: Resource("records", classificationLevel),
            evaluationTime: BusinessHours);

        Assert.Equal(expected, decision.IsAllowed);
    }

    [Theory]
    [InlineData("sample-manager", "sample-manager", "draft", 2, 2, true)]
    [InlineData("sample-manager", "another-user", "draft", 2, 2, false)]
    [InlineData("sample-manager", "sample-manager", "submitted", 2, 2, false)]
    [InlineData("sample-manager", "sample-manager", "draft", 1, 2, false)]
    public async Task Update_enforces_ownership_state_and_clearance(
        string username,
        string ownerUsername,
        string status,
        long clearanceLevel,
        long classificationLevel,
        bool expected)
    {
        var decision = await EvaluateAsync(
            action: "update",
            permissions: ["DOC.UPDATE"],
            subject: Subject("records", clearanceLevel, username),
            resource: Resource(
                "records",
                classificationLevel,
                ownerUsername,
                status),
            evaluationTime: BusinessHours);

        Assert.Equal(expected, decision.IsAllowed);
    }

    [Theory]
    [InlineData("sample-manager", "sample-manager", "draft", 2, 2, true)]
    [InlineData("sample-manager", "another-user", "draft", 2, 2, false)]
    [InlineData("sample-manager", "sample-manager", "submitted", 2, 2, false)]
    [InlineData("sample-manager", "sample-manager", "draft", 1, 2, false)]
    public async Task Submit_enforces_ownership_state_and_clearance(
        string username,
        string ownerUsername,
        string status,
        long clearanceLevel,
        long classificationLevel,
        bool expected)
    {
        var decision = await EvaluateAsync(
            action: "submit",
            permissions: ["WFL.START"],
            subject: Subject("records", clearanceLevel, username),
            resource: Resource(
                "records",
                classificationLevel,
                ownerUsername,
                status),
            evaluationTime: BusinessHours);

        Assert.Equal(expected, decision.IsAllowed);
    }

    [Theory]
    [InlineData("approve", "WFL.APPROVE")]
    [InlineData("reject", "WFL.REJECT")]
    public async Task Workflow_allows_authorized_approver(
        string action,
        string permission)
    {
        var decision = await EvaluateAsync(
            action,
            permissions: [permission],
            roles: ["keycloak:realm:APPROVER"],
            subject: Subject("records", clearanceLevel: 3, "sample-approver"),
            resource: Resource(
                "records",
                classificationLevel: 3,
                ownerUsername: "sample-manager",
                status: "submitted"),
            evaluationTime: BusinessHours);

        Assert.True(decision.IsAllowed);
    }

    [Theory]
    [InlineData(false, true, "records", "records", "sample-approver", "sample-manager", "submitted", 3, 3)]
    [InlineData(true, false, "records", "records", "sample-approver", "sample-manager", "submitted", 3, 3)]
    [InlineData(true, true, "legal", "records", "sample-approver", "sample-manager", "submitted", 3, 3)]
    [InlineData(true, true, "records", "records", "sample-manager", "sample-manager", "submitted", 3, 3)]
    [InlineData(true, true, "records", "records", "sample-approver", "sample-manager", "draft", 3, 3)]
    [InlineData(true, true, "records", "records", "sample-approver", "sample-manager", "submitted", 2, 3)]
    public async Task Approve_denies_when_any_control_fails(
        bool hasPermission,
        bool hasRole,
        string subjectOrganization,
        string resourceOrganization,
        string username,
        string ownerUsername,
        string status,
        long clearanceLevel,
        long classificationLevel)
    {
        var decision = await EvaluateAsync(
            action: "approve",
            permissions: hasPermission ? ["WFL.APPROVE"] : [],
            roles: hasRole ? ["keycloak:realm:APPROVER"] : [],
            subject: Subject(subjectOrganization, clearanceLevel, username),
            resource: Resource(
                resourceOrganization,
                classificationLevel,
                ownerUsername,
                status),
            evaluationTime: BusinessHours);

        Assert.False(decision.IsAllowed);
        Assert.NotEmpty(decision.Failures);
    }

    [Fact]
    public async Task Missing_attributes_fail_closed()
    {
        var decision = await EvaluateAsync(
            action: "read",
            permissions: ["DOC.READ"],
            subject: AuthorizationAttributes.Empty,
            resource: AuthorizationAttributes.Empty,
            evaluationTime: BusinessHours);

        Assert.False(decision.IsAllowed);
        Assert.NotEmpty(decision.Failures);
    }

    private static async Task<AuthorizationDecision> EvaluateAsync(
        string action,
        IEnumerable<string>? permissions = null,
        IEnumerable<string>? roles = null,
        AuthorizationAttributes? subject = null,
        AuthorizationAttributes? resource = null,
        AuthorizationAttributes? context = null,
        DateTimeOffset? evaluationTime = null)
    {
        return await CreateEngine().EvaluateAsync(
            new AuthorizationRequest(
                new AuthorizationSubject(
                    "sample-user",
                    roles,
                    permissions,
                    subject),
                new AuthorizationResource("document", "1", resource),
                action,
                new AuthorizationContext(
                    evaluationTime ?? BusinessHours,
                    context)));
    }

    private static PolicyAuthorizationEngine CreateEngine()
    {
        var manifestPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "document-approval.rulegate.yaml");
        var compilation = new RuleGateManifestCompiler()
            .CompileFromFileAsync(manifestPath)
            .GetAwaiter()
            .GetResult();

        Assert.True(compilation.IsSuccess);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(compilation.Policies),
            new RequirementEvaluationDispatcher(
            [
                new PermissionRequirementEvaluator(),
                new RoleRequirementEvaluator(),
                new AttributeRequirementEvaluator(),
                new AttributeComparisonRequirementEvaluator(),
                new ContextRequirementEvaluator(),
                new TimeWindowRequirementEvaluator(),
                new AllRequirementEvaluator(),
                new AnyRequirementEvaluator(),
                new NotRequirementEvaluator(),
            ]));
    }

    private static AuthorizationAttributes Subject(
        string organizationId,
        long clearanceLevel,
        string username = "sample-user")
    {
        return Attributes(
            ("organizationId", organizationId),
            ("clearanceLevel", clearanceLevel),
            ("username", username));
    }

    private static AuthorizationAttributes Resource(
        string organizationId,
        long classificationLevel,
        string ownerUsername = "sample-manager",
        string status = "draft")
    {
        return Attributes(
            ("organizationId", organizationId),
            ("classificationLevel", classificationLevel),
            ("ownerUsername", ownerUsername),
            ("status", status));
    }

    private static AuthorizationAttributes Attributes(
        params (string Name, object? Value)[] values)
    {
        return new AuthorizationAttributes(
            values.Select(
                static value => new KeyValuePair<string, object?>(
                    value.Name,
                    value.Value)));
    }

    private static AuthorizationAttributes BusinessHoursContext(bool isOpen)
    {
        return Attributes(("organizationBusinessHoursOpen", isOpen));
    }
}
