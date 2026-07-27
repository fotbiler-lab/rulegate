using System.Collections.Concurrent;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class RuleGateLoggingDiagnosticsTests
{
    [Fact]
    public void
        AddRuleGate_does_not_enable_diagnostics_by_default()
    {
        var services = new ServiceCollection();

        services.AddRuleGate();

        Assert.DoesNotContain(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(
                    IAuthorizationDiagnosticsSink));
    }

    [Fact]
    public void
        AddLoggingDiagnostics_requires_builder()
    {
        RuleGateBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(
            () => builder.AddLoggingDiagnostics());
    }

    [Fact]
    public void
        AddLoggingDiagnostics_is_idempotent_and_singleton()
    {
        var services = new ServiceCollection();

        var builder = services.AddRuleGate();

        var first =
            builder.AddLoggingDiagnostics();

        var second =
            builder.AddLoggingDiagnostics();

        Assert.Same(builder, first);
        Assert.Same(builder, second);

        var descriptor =
            Assert.Single(
                services,
                candidate =>
                    candidate.ServiceType ==
                    typeof(
                        IAuthorizationDiagnosticsSink));

        Assert.Equal(
            ServiceLifetime.Singleton,
            descriptor.Lifetime);

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        var firstSink =
            serviceProvider.GetRequiredService<
                IAuthorizationDiagnosticsSink>();

        var secondSink =
            serviceProvider.GetRequiredService<
                IAuthorizationDiagnosticsSink>();

        Assert.Same(firstSink, secondSink);
        Assert.True(
            firstSink.GetType().Name.Contains(
                "LoggingAuthorizationDiagnosticsSink",
                StringComparison.Ordinal));
    }

    [Fact]
    public void
        AddLoggingDiagnostics_preserves_custom_sink()
    {
        var services = new ServiceCollection();
        var expected = new StubDiagnosticsSink();

        services.AddSingleton<
            IAuthorizationDiagnosticsSink>(expected);

        services
            .AddRuleGate()
            .AddLoggingDiagnostics();

        using var serviceProvider =
            services.BuildServiceProvider();

        var actual =
            serviceProvider.GetRequiredService<
                IAuthorizationDiagnosticsSink>();

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task
        LoggingDiagnostics_records_safe_engine_output()
    {
        var recordingProvider =
            new RecordingLoggerProvider();

        var services = new ServiceCollection();

        services.AddLogging(
            logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(
                    LogLevel.Debug);
                logging.AddProvider(
                    recordingProvider);
            });

        services
            .AddRuleGate()
            .AddLoggingDiagnostics()
            .AddPolicy(
                new PolicyDefinition(
                    "sample-read",
                    "sample-resource",
                    "read",
                    new PermissionRequirementDefinition(
                        "sample.read",
                        "permission-node")));

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        var engine =
            serviceProvider.GetRequiredService<
                IAuthorizationEngine>();

        var decision =
            await engine.EvaluateAsync(
                CreateRequest(
                [
                    "sample.read"
                ]));

        Assert.True(decision.IsAllowed);

        var entries =
            recordingProvider.Entries;

        Assert.Contains(
            entries,
            entry =>
                entry.Level == LogLevel.Information &&
                entry.EventId.Id == 2000 &&
                entry.Message.Contains(
                    "sample-read",
                    StringComparison.Ordinal) &&
                entry.Message.Contains(
                    "True",
                    StringComparison.Ordinal));

        Assert.Contains(
            entries,
            entry =>
                entry.Level == LogLevel.Debug &&
                entry.EventId.Id == 2001 &&
                entry.Message.Contains(
                    "permission-node",
                    StringComparison.Ordinal) &&
                entry.Message.Contains(
                    "Permission",
                    StringComparison.Ordinal));
    }

    [Fact]
    public async Task
        LoggingDiagnostics_does_not_log_attribute_name()
    {
        const string sensitiveAttributeName =
            "internal-security-clearance";

        var recordingProvider =
            new RecordingLoggerProvider();

        var services = new ServiceCollection();

        services.AddLogging(
            logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(
                    LogLevel.Debug);
                logging.AddProvider(
                    recordingProvider);
            });

        services
            .AddRuleGate()
            .AddLoggingDiagnostics();

        using var serviceProvider =
            services.BuildServiceProvider();

        var sink =
            serviceProvider.GetRequiredService<
                IAuthorizationDiagnosticsSink>();

        var requirementDiagnostic =
            new RequirementEvaluationDiagnostic(
                Guid.NewGuid(),
                parentEvaluationId: null,
                requirementId: "attribute-node",
                AuthorizationRequirementKind.Attribute,
                RequirementEvaluationOutcome.Satisfied,
                TimeSpan.Zero,
                [],
                AuthorizationAttributeSource.Resource,
                sensitiveAttributeName);

        await sink.WriteAsync(
            new AuthorizationEvaluationDiagnostic(
                Guid.NewGuid(),
                "safe-policy",
                isAllowed: true,
                TimeSpan.Zero,
                [],
                [
                    requirementDiagnostic
                ]));

        Assert.DoesNotContain(
            recordingProvider.Entries,
            entry =>
                entry.Message.Contains(
                    sensitiveAttributeName,
                    StringComparison.Ordinal));

        Assert.Contains(
            recordingProvider.Entries,
            entry =>
                entry.EventId.Id == 2001 &&
                entry.Message.Contains(
                    "Resource",
                    StringComparison.Ordinal));
    }

    private static AuthorizationRequest CreateRequest(
        IEnumerable<string> permissions)
    {
        return new AuthorizationRequest(
            new AuthorizationSubject(
                "user-1",
                permissions: permissions),

            new AuthorizationResource(
                "sample-resource",
                "resource-1"),

            "read",

            new AuthorizationContext(
                DateTimeOffset.UnixEpoch));
    }

    private sealed class StubDiagnosticsSink
        : IAuthorizationDiagnosticsSink
    {
        public bool IsEnabled => false;

        public ValueTask WriteAsync(
            AuthorizationEvaluationDiagnostic diagnostic,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingLoggerProvider
        : ILoggerProvider
    {
        private readonly ConcurrentQueue<RecordedLogEntry>
            _entries = new();

        internal IReadOnlyList<RecordedLogEntry>
            Entries => _entries.ToArray();

        public ILogger CreateLogger(
            string categoryName)
        {
            return new RecordingLogger(
                categoryName,
                _entries);
        }

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger
        : ILogger
    {
        private readonly string _categoryName;

        private readonly ConcurrentQueue<
            RecordedLogEntry> _entries;

        internal RecordingLogger(
            string categoryName,
            ConcurrentQueue<RecordedLogEntry> entries)
        {
            _categoryName = categoryName;
            _entries = entries;
        }

        public IDisposable? BeginScope<TState>(
            TState state)
            where TState : notnull
        {
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(
            LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            _entries.Enqueue(
                new RecordedLogEntry(
                    _categoryName,
                    logLevel,
                    eventId,
                    formatter(state, exception)));
        }
    }

    private sealed class NoopDisposable
        : IDisposable
    {
        internal static NoopDisposable Instance
        {
            get;
        } = new();

        public void Dispose()
        {
        }
    }

    private sealed record RecordedLogEntry(
        string CategoryName,
        LogLevel Level,
        EventId EventId,
        string Message);
}
