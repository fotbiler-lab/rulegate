using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.PolicySources;
using Fotbiler.RuleGate.Core.Policies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class PolicySourceDependencyInjectionTests
{
    [Fact]
    public async Task AddYamlPolicyFile_UsesAtomicProviderAndLoadsPolicies()
    {
        var path = await CreateManifestAsync(
            "document-read",
            "read");

        try
        {
            var services = CreateServices();

            services
                .AddRuleGate()
                .AddYamlPolicyFile(path);

            using var serviceProvider =
                services.BuildServiceProvider(
                    new ServiceProviderOptions
                    {
                        ValidateOnBuild = true,
                        ValidateScopes = true,
                    });

            var policyProvider =
                serviceProvider.GetRequiredService<
                    IPolicyProvider>();
            var reloadService =
                serviceProvider.GetRequiredService<
                    IPolicyReloadService>();

            Assert.IsType<AtomicPolicyProvider>(
                policyProvider);
            Assert.Same(policyProvider, reloadService);

            var policy = await policyProvider.FindAsync(
                "document",
                "read");

            Assert.Equal("document-read", policy?.Id);
            Assert.Equal(1, reloadService.CurrentSnapshot.Version);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void PolicySourceRegistration_IsIdempotentForInfrastructure()
    {
        var services = CreateServices();
        var builder = services.AddRuleGate();

        builder
            .AddPolicySource<EmptyPolicySource>()
            .AddPolicySource<EmptyPolicySource>();

        Assert.Single(
            services,
            descriptor => descriptor.ServiceType ==
                typeof(IPolicyProvider));
        Assert.Single(
            services,
            descriptor => descriptor.ServiceType ==
                typeof(IPolicyReloadService));
        Assert.Single(
            services,
            descriptor => descriptor.ServiceType ==
                typeof(IPolicySource));
        Assert.Single(
            services,
            descriptor => descriptor.ServiceType ==
                typeof(IHostedService));
    }

    [Fact]
    public async Task AddPolicySource_AcceptsApplicationDefinedSource()
    {
        var policy = CreatePolicy(
            "application-defined");
        var source = new FixedPolicySource(
            "application",
            [policy]);
        var services = CreateServices();

        services
            .AddRuleGate()
            .AddPolicySource(source);

        using var serviceProvider =
            services.BuildServiceProvider();

        var actual = await serviceProvider
            .GetRequiredService<IPolicyProvider>()
            .FindAsync("document", "read");

        Assert.Same(policy, actual);
    }

    [Fact]
    public async Task ConfigurationPolicySource_BindsStructuredConfiguration()
    {
        var configuration = CreateConfiguration(
            "document.read");
        var source = new ConfigurationPolicySource(
            configuration,
            "RuleGate",
            new ConfigurationPolicySourceOptions
            {
                ReloadOnChange = true,
            });

        var first = await source.LoadAsync();

        Assert.True(first.IsSuccess);
        Assert.Equal(
            "document-read",
            Assert.Single(first.Policies).Id);
        Assert.True(source.ReloadOnChange);

        configuration[
            "RuleGate:Policies:0:Requirement:Permission"] =
            "document.read.updated";

        var second = await source.LoadAsync();
        var requirement = Assert.IsType<
            PermissionRequirementDefinition>(
                Assert.Single(second.Policies).Requirement);

        Assert.Equal(
            "document.read.updated",
            requirement.Permission);
    }

    [Fact]
    public async Task ConfigurationPolicySource_MissingSectionFailsClosed()
    {
        var configuration =
            new ConfigurationBuilder().Build();
        var source = new ConfigurationPolicySource(
            configuration,
            "RuleGate");

        var result = await source.LoadAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ConfigurationPolicySourceCodes
                .SectionNotFound,
            Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public async Task ConfigurationPolicySource_RejectsUnknownProperties()
    {
        var configuration = CreateConfiguration(
            "document.read");
        configuration["RuleGate:UnknownProperty"] =
            "unexpected";
        var source = new ConfigurationPolicySource(
            configuration,
            "RuleGate");

        var result = await source.LoadAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ConfigurationPolicySourceCodes.BindingFailed,
            Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public async Task ReloadOnChange_ActivatesValidYamlReplacement()
    {
        var path = await CreateManifestAsync(
            "document-read-v1",
            "read");

        try
        {
            var services = CreateServices();

            services
                .AddRuleGate()
                .AddYamlPolicyFile(
                    path,
                    options =>
                        options.ReloadOnChange = true);

            await using var serviceProvider =
                services.BuildServiceProvider();

            var hostedService = serviceProvider
                .GetServices<IHostedService>()
                .Single();
            var reloadService = serviceProvider
                .GetRequiredService<IPolicyReloadService>();
            var policyProvider = serviceProvider
                .GetRequiredService<IPolicyProvider>();

            await hostedService.StartAsync(
                CancellationToken.None);

            await WaitUntilAsync(
                () => reloadService.CurrentSnapshot.Version >= 1);

            var initialVersion =
                reloadService.CurrentSnapshot.Version;

            await File.WriteAllTextAsync(
                path,
                CreateManifest(
                    "document-write-v2",
                    "write"));

            await WaitUntilAsync(
                () => reloadService.CurrentSnapshot.Version >
                    initialVersion);

            Assert.Null(
                await policyProvider.FindAsync(
                    "document",
                    "read"));
            Assert.Equal(
                "document-write-v2",
                (await policyProvider.FindAsync(
                    "document",
                    "write"))?.Id);

            await hostedService.StopAsync(
                CancellationToken.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReloadOnChange_ActivatesConfigurationReplacement()
    {
        var configuration = CreateConfiguration(
            "document.read");
        var services = CreateServices();

        services
            .AddRuleGate()
            .AddConfigurationPolicySource(
                configuration,
                "RuleGate",
                options =>
                    options.ReloadOnChange = true);

        await using var serviceProvider =
            services.BuildServiceProvider();

        var hostedService = serviceProvider
            .GetServices<IHostedService>()
            .Single();
        var reloadService = serviceProvider
            .GetRequiredService<IPolicyReloadService>();
        var policyProvider = serviceProvider
            .GetRequiredService<IPolicyProvider>();

        await hostedService.StartAsync(
            CancellationToken.None);

        await WaitUntilAsync(
            () => reloadService.CurrentSnapshot.Version >= 1);

        var initialVersion =
            reloadService.CurrentSnapshot.Version;

        configuration["RuleGate:Policies:0:Id"] =
            "document-write";
        configuration["RuleGate:Policies:0:Action"] =
            "write";
        configuration[
            "RuleGate:Policies:0:Requirement:Permission"] =
            "document.write";
        configuration.Reload();

        await WaitUntilAsync(
            () => reloadService.CurrentSnapshot.Version >
                initialVersion);

        Assert.Null(
            await policyProvider.FindAsync(
                "document",
                "read"));
        Assert.Equal(
            "document-write",
            (await policyProvider.FindAsync(
                "document",
                "write"))?.Id);

        await hostedService.StopAsync(
            CancellationToken.None);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore();

        return services;
    }

    private static IConfigurationRoot CreateConfiguration(
        string permission)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RuleGate:SchemaVersion"] = "1",
                    ["RuleGate:Application:Id"] =
                        "configuration-test",
                    ["RuleGate:Application:Name"] =
                        "Configuration Test",
                    ["RuleGate:Policies:0:Id"] =
                        "document-read",
                    ["RuleGate:Policies:0:ResourceType"] =
                        "document",
                    ["RuleGate:Policies:0:Action"] =
                        "read",
                    ["RuleGate:Policies:0:Requirement:Permission"] =
                        permission,
                })
            .Build();
    }

    private static async Task<string> CreateManifestAsync(
        string id,
        string action)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"rulegate-reload-{Guid.NewGuid():N}.yaml");

        await File.WriteAllTextAsync(
            path,
            CreateManifest(id, action));

        return path;
    }

    private static string CreateManifest(
        string id,
        string action)
    {
        return $$"""
            schemaVersion: 1
            application:
              id: reload-test
              name: Reload Test
            policies:
              - id: {{id}}
                resourceType: document
                action: {{action}}
                requirement:
                  permission: document.{{action}}
            """;
    }

    private static PolicyDefinition CreatePolicy(
        string id)
    {
        return new PolicyDefinition(
            id,
            "document",
            "read",
            new PermissionRequirementDefinition(
                "document.read"));
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition)
    {
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(5));

        while (!condition())
        {
            await Task.Delay(50, timeout.Token);
        }
    }

    public sealed class EmptyPolicySource : IPolicySource
    {
        public string Name => "empty";

        public ValueTask<PolicySourceLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                PolicySourceLoadResult.Success([]));
        }
    }

    private sealed class FixedPolicySource : IPolicySource
    {
        private readonly IReadOnlyList<PolicyDefinition>
            _policies;

        public FixedPolicySource(
            string name,
            IEnumerable<PolicyDefinition> policies)
        {
            Name = name;
            _policies = policies.ToArray();
        }

        public string Name { get; }

        public ValueTask<PolicySourceLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                PolicySourceLoadResult.Success(_policies));
        }
    }
}
