using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationServiceExtensionsTests
{
    [Fact]
    public async Task
        AuthorizationResourceOverloadBuildsPolicyName()
    {
        var expectedResult =
            AuthorizationResult.Success();

        var service =
            new RecordingAuthorizationService(
                expectedResult);

        var user =
            CreatePrincipal();

        var resource =
            new AuthorizationResource(
                "document",
                "document-1");

        var result =
            await service.AuthorizeRuleGateAsync(
                user,
                resource,
                action: "read");

        Assert.Same(
            expectedResult,
            result);

        var invocation =
            Assert.Single(
                service.PolicyInvocations);

        Assert.Same(
            user,
            invocation.User);

        Assert.Same(
            resource,
            invocation.Resource);

        Assert.Equal(
            "RuleGate:document:read",
            invocation.PolicyName);

        Assert.Equal(
            0,
            service.RequirementInvocationCount);
    }

    [Fact]
    public async Task
        ExplicitResourceTypeOverloadBuildsPolicyName()
    {
        var expectedResult =
            AuthorizationResult.Failed();

        var service =
            new RecordingAuthorizationService(
                expectedResult);

        var user =
            CreatePrincipal();

        var resource =
            new object();

        var result =
            await service.AuthorizeRuleGateAsync(
                user,
                resource,
                resourceType: "invoice",
                action: "approve");

        Assert.Same(
            expectedResult,
            result);

        var invocation =
            Assert.Single(
                service.PolicyInvocations);

        Assert.Same(
            user,
            invocation.User);

        Assert.Same(
            resource,
            invocation.Resource);

        Assert.Equal(
            "RuleGate:invoice:approve",
            invocation.PolicyName);
    }

    [Fact]
    public async Task
        NullAuthorizationServiceIsRejected()
    {
        var user =
            CreatePrincipal();

        var resource =
            new AuthorizationResource(
                "document");

        var exception =
            await Assert.ThrowsAsync<
                ArgumentNullException>(
                    () =>
                        RuleGateAuthorizationServiceExtensions
                            .AuthorizeRuleGateAsync(
                                authorizationService:
                                    null!,
                                user,
                                resource,
                                action: "read"));

        Assert.Equal(
            "authorizationService",
            exception.ParamName);
    }

    [Fact]
    public async Task NullUserIsRejected()
    {
        var service =
            new RecordingAuthorizationService(
                AuthorizationResult.Success());

        var resource =
            new AuthorizationResource(
                "document");

        var exception =
            await Assert.ThrowsAsync<
                ArgumentNullException>(
                    () =>
                        service.AuthorizeRuleGateAsync(
                            user: null!,
                            resource,
                            action: "read"));

        Assert.Equal(
            "user",
            exception.ParamName);

        Assert.Empty(
            service.PolicyInvocations);
    }

    [Fact]
    public async Task NullResourceIsRejected()
    {
        var service =
            new RecordingAuthorizationService(
                AuthorizationResult.Success());

        var exception =
            await Assert.ThrowsAsync<
                ArgumentNullException>(
                    () =>
                        service.AuthorizeRuleGateAsync(
                            CreatePrincipal(),
                            resource: null!,
                            action: "read"));

        Assert.Equal(
            "resource",
            exception.ParamName);

        Assert.Empty(
            service.PolicyInvocations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("read all")]
    [InlineData("read:private")]
    public async Task
        InvalidActionIsRejectedBeforeAuthorization(
            string? action)
    {
        var service =
            new RecordingAuthorizationService(
                AuthorizationResult.Success());

        var resource =
            new AuthorizationResource(
                "document");

        await Assert.ThrowsAnyAsync<
            ArgumentException>(
                () =>
                    service.AuthorizeRuleGateAsync(
                        CreatePrincipal(),
                        resource,
                        action!));

        Assert.Empty(
            service.PolicyInvocations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("document type")]
    [InlineData("document:private")]
    public async Task
        InvalidExplicitResourceTypeIsRejectedBeforeAuthorization(
            string? resourceType)
    {
        var service =
            new RecordingAuthorizationService(
                AuthorizationResult.Success());

        await Assert.ThrowsAnyAsync<
            ArgumentException>(
                () =>
                    service.AuthorizeRuleGateAsync(
                        CreatePrincipal(),
                        new object(),
                        resourceType!,
                        action: "read"));

        Assert.Empty(
            service.PolicyInvocations);
    }

    private static ClaimsPrincipal
        CreatePrincipal()
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                authenticationType: "Test"));
    }

    private sealed class
        RecordingAuthorizationService
        : IAuthorizationService
    {
        private readonly AuthorizationResult
            _result;

        public RecordingAuthorizationService(
            AuthorizationResult result)
        {
            _result = result;
        }

        public List<PolicyInvocation>
            PolicyInvocations
        { get; } = [];

        public int RequirementInvocationCount
        {
            get;
            private set;
        }

        public Task<AuthorizationResult>
            AuthorizeAsync(
                ClaimsPrincipal user,
                object? resource,
                IEnumerable<
                    IAuthorizationRequirement>
                    requirements)
        {
            RequirementInvocationCount++;

            return Task.FromResult(
                _result);
        }

        public Task<AuthorizationResult>
            AuthorizeAsync(
                ClaimsPrincipal user,
                object? resource,
                string policyName)
        {
            PolicyInvocations.Add(
                new PolicyInvocation(
                    user,
                    resource,
                    policyName));

            return Task.FromResult(
                _result);
        }
    }

    private sealed record PolicyInvocation(
        ClaimsPrincipal User,
        object? Resource,
        string PolicyName);
}
