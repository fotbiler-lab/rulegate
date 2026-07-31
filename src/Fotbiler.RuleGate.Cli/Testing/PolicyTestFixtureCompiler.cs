using System.Globalization;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Fotbiler.RuleGate.Cli.Testing;

internal sealed class PolicyTestFixtureCompiler
{
    private const int MaximumRecursion = 64;
    private const int MaximumTestCount = 10_000;

    private static readonly string[] OffsetDateTimeFormats =
    [
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"
    ];

    private static readonly string[] UniversalDateTimeFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
    ];

    private readonly IDeserializer _deserializer;

    public PolicyTestFixtureCompiler()
    {
        _deserializer =
            new DeserializerBuilder()
                .WithNamingConvention(
                    CamelCaseNamingConvention.Instance)
                .WithDuplicateKeyChecking()
                .WithMaximumRecursion(
                    MaximumRecursion)
                .Build();
    }

    public async ValueTask<PolicyTestFixtureCompilation>
        CompileFromFileAsync(
            string path,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        cancellationToken.ThrowIfCancellationRequested();

        string yaml;

        try
        {
            yaml = await File.ReadAllTextAsync(
                path,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException exception)
        {
            return Failure(
                PolicyTestDiagnosticCodes.FileNotFound,
                exception.Message);
        }
        catch (DirectoryNotFoundException exception)
        {
            return Failure(
                PolicyTestDiagnosticCodes.FileNotFound,
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(
                PolicyTestDiagnosticCodes.FileReadFailed,
                exception.Message);
        }
        catch (IOException exception)
        {
            return Failure(
                PolicyTestDiagnosticCodes.FileReadFailed,
                exception.Message);
        }

        if (string.IsNullOrWhiteSpace(yaml))
        {
            return Failure(
                PolicyTestDiagnosticCodes.EmptyContent,
                "Policy test fixture YAML content is empty.");
        }

        PolicyTestFixture? fixture;

        try
        {
            using var reader =
                new StringReader(yaml);

            fixture =
                _deserializer.Deserialize<
                    PolicyTestFixture>(reader);
        }
        catch (YamlException exception)
        {
            return new PolicyTestFixtureCompilation(
                Suite: null,
                Errors:
                [
                    new PolicyTestDiagnostic(
                        Category: "fixture",
                        Code:
                            PolicyTestDiagnosticCodes
                                .InvalidYaml,
                        Message:
                            exception.InnerException?.Message ??
                            exception.Message,
                        Line: GetPosition(
                            exception.Start.Line),
                        Column: GetPosition(
                            exception.Start.Column))
                ]);
        }

        if (fixture is null)
        {
            return Failure(
                PolicyTestDiagnosticCodes.InvalidFixture,
                "Policy test fixture YAML must contain a root object.");
        }

        return Compile(
            Path.GetFullPath(path),
            fixture);
    }

    private static PolicyTestFixtureCompilation Compile(
        string fixturePath,
        PolicyTestFixture fixture)
    {
        var errors =
            new List<PolicyTestDiagnostic>();

        if (fixture.SchemaVersion !=
            PolicyTestDefaults.SupportedSchemaVersion)
        {
            AddError(
                errors,
                "schemaVersion",
                $"schemaVersion must be {PolicyTestDefaults.SupportedSchemaVersion}.");
        }

        string? manifestPath = null;

        if (string.IsNullOrWhiteSpace(
                fixture.Manifest))
        {
            AddError(
                errors,
                "manifest",
                "manifest is required.");
        }
        else
        {
            try
            {
                manifestPath =
                    Path.GetFullPath(
                        fixture.Manifest,
                        Path.GetDirectoryName(
                            fixturePath)!);
            }
            catch (Exception exception)
                when (exception is ArgumentException or
                      NotSupportedException or
                      PathTooLongException)
            {
                AddError(
                    errors,
                    "manifest",
                    "manifest must be a valid relative or absolute file path.");
            }
        }

        if (fixture.Tests is null ||
            fixture.Tests.Count == 0)
        {
            AddError(
                errors,
                "tests",
                "At least one policy test is required.");
        }
        else if (fixture.Tests.Count >
                 MaximumTestCount)
        {
            AddError(
                errors,
                "tests",
                $"A fixture cannot contain more than {MaximumTestCount} tests.");
        }

        var tests =
            new List<PolicyTestCase>();

        var testIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        if (fixture.Tests is not null &&
            fixture.Tests.Count <=
            MaximumTestCount)
        {
            for (var index = 0;
                 index < fixture.Tests.Count;
                 index++)
            {
                CompileTest(
                    fixture.Tests[index],
                    index,
                    testIds,
                    tests,
                    errors);
            }
        }

        if (errors.Count != 0 ||
            manifestPath is null)
        {
            return new PolicyTestFixtureCompilation(
                Suite: null,
                Errors: errors.AsReadOnly());
        }

        return new PolicyTestFixtureCompilation(
            Suite:
                new PolicyTestSuite(
                    fixturePath,
                    manifestPath,
                    tests.AsReadOnly()),
            Errors:
                Array.Empty<PolicyTestDiagnostic>());
    }

    private static void CompileTest(
        PolicyTestCaseFixture? fixture,
        int index,
        HashSet<string> testIds,
        List<PolicyTestCase> tests,
        List<PolicyTestDiagnostic> errors)
    {
        var path = $"tests[{index}]";

        if (fixture is null)
        {
            AddError(
                errors,
                path,
                "Test entry cannot be null.");
            return;
        }

        var errorCount = errors.Count;

        var id =
            RequireText(
                fixture.Id,
                $"{path}.id",
                errors);

        if (id is not null &&
            !testIds.Add(id))
        {
            AddError(
                errors,
                $"{path}.id",
                $"Test identifier '{id}' is duplicated.");
        }

        var request =
            CompileRequest(
                fixture.Request,
                $"{path}.request",
                errors);

        var expectation =
            CompileExpectation(
                fixture.Expect,
                $"{path}.expect",
                errors);

        if (errors.Count != errorCount ||
            id is null ||
            request is null ||
            expectation is null)
        {
            return;
        }

        tests.Add(
            new PolicyTestCase(
                id,
                string.IsNullOrWhiteSpace(
                    fixture.Description)
                    ? null
                    : fixture.Description,
                request,
                expectation.Value.Outcome,
                expectation.Value.FailureCodes));
    }

    private static AuthorizationRequest? CompileRequest(
        PolicyTestRequestFixture? fixture,
        string path,
        List<PolicyTestDiagnostic> errors)
    {
        if (fixture is null)
        {
            AddError(
                errors,
                path,
                "request is required.");
            return null;
        }

        var subject =
            CompileSubject(
                fixture.Subject,
                $"{path}.subject",
                errors);

        var resource =
            CompileResource(
                fixture.Resource,
                $"{path}.resource",
                errors);

        var action =
            RequireText(
                fixture.Action,
                $"{path}.action",
                errors);

        var context =
            CompileContext(
                fixture.Context,
                $"{path}.context",
                errors);

        return subject is null ||
               resource is null ||
               action is null ||
               context is null
            ? null
            : new AuthorizationRequest(
                subject,
                resource,
                action,
                context);
    }

    private static AuthorizationSubject? CompileSubject(
        PolicyTestSubjectFixture? fixture,
        string path,
        List<PolicyTestDiagnostic> errors)
    {
        if (fixture is null)
        {
            AddError(
                errors,
                path,
                "subject is required.");
            return null;
        }

        var id =
            RequireText(
                fixture.Id,
                $"{path}.id",
                errors);

        var roles =
            CompileStringSet(
                fixture.Roles,
                $"{path}.roles",
                errors);

        var permissions =
            CompileStringSet(
                fixture.Permissions,
                $"{path}.permissions",
                errors);

        var attributes =
            CompileAttributes(
                fixture.Attributes,
                $"{path}.attributes",
                errors);

        return id is null ||
               roles is null ||
               permissions is null ||
               attributes is null
            ? null
            : new AuthorizationSubject(
                id,
                roles,
                permissions,
                attributes);
    }

    private static AuthorizationResource? CompileResource(
        PolicyTestResourceFixture? fixture,
        string path,
        List<PolicyTestDiagnostic> errors)
    {
        if (fixture is null)
        {
            AddError(
                errors,
                path,
                "resource is required.");
            return null;
        }

        var type =
            RequireText(
                fixture.Type,
                $"{path}.type",
                errors);

        if (fixture.Id is not null &&
            string.IsNullOrWhiteSpace(fixture.Id))
        {
            AddError(
                errors,
                $"{path}.id",
                "resource id cannot be blank.");
        }

        var hasValidId =
            fixture.Id is null ||
            !string.IsNullOrWhiteSpace(fixture.Id);

        var attributes =
            CompileAttributes(
                fixture.Attributes,
                $"{path}.attributes",
                errors);

        return type is null ||
               !hasValidId ||
               attributes is null
            ? null
            : new AuthorizationResource(
                type,
                fixture.Id,
                attributes);
    }

    private static AuthorizationContext? CompileContext(
        PolicyTestContextFixture? fixture,
        string path,
        List<PolicyTestDiagnostic> errors)
    {
        if (fixture is null)
        {
            AddError(
                errors,
                path,
                "context is required.");
            return null;
        }

        if (!TryParseDateTimeOffset(
                fixture.EvaluationTime,
                out var evaluationTime))
        {
            AddError(
                errors,
                $"{path}.evaluationTime",
                "evaluationTime must be an ISO 8601 date and time with an explicit offset.");
        }

        var attributes =
            CompileAttributes(
                fixture.Attributes,
                $"{path}.attributes",
                errors);

        return attributes is null ||
               !TryParseDateTimeOffset(
                   fixture.EvaluationTime,
                   out evaluationTime)
            ? null
            : new AuthorizationContext(
                evaluationTime,
                attributes);
    }

    private static AuthorizationAttributes? CompileAttributes(
        List<PolicyTestAttributeFixture?>? fixtures,
        string path,
        List<PolicyTestDiagnostic> errors)
    {
        if (fixtures is null)
        {
            return AuthorizationAttributes.Empty;
        }

        var errorCount = errors.Count;
        var values =
            new List<KeyValuePair<string, object?>>();
        var names =
            new HashSet<string>(
                StringComparer.Ordinal);

        for (var index = 0;
             index < fixtures.Count;
             index++)
        {
            var item = fixtures[index];
            var itemPath = $"{path}[{index}]";

            if (item is null)
            {
                AddError(
                    errors,
                    itemPath,
                    "Attribute entry cannot be null.");
                continue;
            }

            var name =
                RequireText(
                    item.Name,
                    $"{itemPath}.name",
                    errors);

            if (name is not null &&
                !names.Add(name))
            {
                AddError(
                    errors,
                    $"{itemPath}.name",
                    $"Attribute name '{name}' is duplicated.");
            }

            if (!TryCompileAttributeValue(
                    item,
                    itemPath,
                    errors,
                    out var value))
            {
                continue;
            }

            if (name is not null)
            {
                values.Add(
                    new KeyValuePair<string, object?>(
                        name,
                        value));
            }
        }

        return errors.Count == errorCount
            ? new AuthorizationAttributes(values)
            : null;
    }

    private static bool TryCompileAttributeValue(
        PolicyTestAttributeFixture fixture,
        string path,
        List<PolicyTestDiagnostic> errors,
        out object? value)
    {
        value = null;

        switch (fixture.ValueType)
        {
            case "nullValue":
                if (fixture.Value is not null ||
                    fixture.Values is not null)
                {
                    return AttributeError(
                        errors,
                        path,
                        "nullValue cannot define value or values.");
                }

                return true;

            case "string":
                if (fixture.Value is null ||
                    fixture.Values is not null)
                {
                    return ScalarError(errors, path);
                }

                value = fixture.Value;
                return true;

            case "boolean":
                if (fixture.Value is null ||
                    fixture.Values is not null ||
                    !bool.TryParse(
                        fixture.Value,
                        out var booleanValue))
                {
                    return AttributeError(
                        errors,
                        path,
                        "boolean requires one true or false value.");
                }

                value = booleanValue;
                return true;

            case "number":
                if (fixture.Value is null ||
                    fixture.Values is not null ||
                    !decimal.TryParse(
                        fixture.Value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var numberValue))
                {
                    return AttributeError(
                        errors,
                        path,
                        "number requires one invariant decimal value.");
                }

                value = numberValue;
                return true;

            case "dateTimeOffset":
                if (fixture.Values is not null ||
                    !TryParseDateTimeOffset(
                        fixture.Value,
                        out var dateTimeValue))
                {
                    return AttributeError(
                        errors,
                        path,
                        "dateTimeOffset requires one ISO 8601 value with an explicit offset.");
                }

                value = dateTimeValue;
                return true;

            case "stringCollection":
                return TryCompileCollection(
                    fixture,
                    path,
                    errors,
                    static item => (true, (object)item),
                    out value);

            case "booleanCollection":
                return TryCompileCollection(
                    fixture,
                    path,
                    errors,
                    static item =>
                        bool.TryParse(item, out var parsed)
                            ? (true, (object)parsed)
                            : (false, null!),
                    out value);

            case "numberCollection":
                return TryCompileCollection(
                    fixture,
                    path,
                    errors,
                    static item =>
                        decimal.TryParse(
                            item,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out var parsed)
                            ? (true, (object)parsed)
                            : (false, null!),
                    out value);

            case "dateTimeOffsetCollection":
                return TryCompileCollection(
                    fixture,
                    path,
                    errors,
                    static item =>
                        TryParseDateTimeOffset(
                            item,
                            out var parsed)
                            ? (true, (object)parsed)
                            : (false, null!),
                    out value);

            default:
                return AttributeError(
                    errors,
                    $"{path}.valueType",
                    "valueType must be nullValue, string, boolean, number, dateTimeOffset, stringCollection, booleanCollection, numberCollection, or dateTimeOffsetCollection.");
        }
    }

    private static bool TryCompileCollection(
        PolicyTestAttributeFixture fixture,
        string path,
        List<PolicyTestDiagnostic> errors,
        Func<string, (bool Success, object Value)> parser,
        out object? value)
    {
        value = null;

        if (fixture.Value is not null ||
            fixture.Values is null)
        {
            return AttributeError(
                errors,
                path,
                "Collection value types require values and cannot define value.");
        }

        if (fixture.Values.Count >
            AuthorizationAttributeValue
                .MaximumCollectionElementCount)
        {
            return AttributeError(
                errors,
                $"{path}.values",
                $"Attribute collections cannot contain more than {AuthorizationAttributeValue.MaximumCollectionElementCount} elements.");
        }

        var items =
            new List<object>(
                fixture.Values.Count);

        for (var index = 0;
             index < fixture.Values.Count;
             index++)
        {
            var item = fixture.Values[index];

            if (item is null)
            {
                return AttributeError(
                    errors,
                    $"{path}.values[{index}]",
                    "Collection values cannot be null.");
            }

            var parsed = parser(item);

            if (!parsed.Success)
            {
                return AttributeError(
                    errors,
                    $"{path}.values[{index}]",
                    "Collection item does not match valueType.");
            }

            items.Add(parsed.Value);
        }

        value = items;
        return true;
    }

    private static (
        string Outcome,
        IReadOnlyList<string>? FailureCodes)?
        CompileExpectation(
            PolicyTestExpectationFixture? fixture,
            string path,
            List<PolicyTestDiagnostic> errors)
    {
        if (fixture is null)
        {
            AddError(
                errors,
                path,
                "expect is required.");
            return null;
        }

        if (fixture.Outcome is not (
            "allow" or
            "deny" or
            "indeterminate"))
        {
            AddError(
                errors,
                $"{path}.outcome",
                "outcome must be allow, deny, or indeterminate.");
        }

        var failureCodes =
            fixture.FailureCodes is null
                ? null
                : CompileStringSet(
                    fixture.FailureCodes,
                    $"{path}.failureCodes",
                    errors);

        if (fixture.Outcome == "allow" &&
            failureCodes is { Count: > 0 })
        {
            AddError(
                errors,
                $"{path}.failureCodes",
                "An allow expectation cannot include failure codes.");
        }

        return fixture.Outcome is not (
                   "allow" or
                   "deny" or
                   "indeterminate") ||
            fixture.FailureCodes is not null &&
            failureCodes is null
            ? null
            : (
                fixture.Outcome,
                failureCodes?
                    .Order(
                        StringComparer.Ordinal)
                    .ToArray());
    }

    private static IReadOnlyList<string>?
        CompileStringSet(
            List<string?>? values,
            string path,
            List<PolicyTestDiagnostic> errors)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        var errorCount = errors.Count;
        var result = new List<string>();
        var unique =
            new HashSet<string>(
                StringComparer.Ordinal);

        for (var index = 0;
             index < values.Count;
             index++)
        {
            var itemPath = $"{path}[{index}]";
            var value =
                RequireText(
                    values[index],
                    itemPath,
                    errors);

            if (value is not null &&
                !unique.Add(value))
            {
                AddError(
                    errors,
                    itemPath,
                    $"Value '{value}' is duplicated.");
            }

            if (value is not null)
            {
                result.Add(value);
            }
        }

        return errors.Count == errorCount
            ? result.AsReadOnly()
            : null;
    }

    private static string? RequireText(
        string? value,
        string path,
        List<PolicyTestDiagnostic> errors)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        AddError(
            errors,
            path,
            "A non-empty value is required.");

        return null;
    }

    private static bool TryParseDateTimeOffset(
        string? value,
        out DateTimeOffset result)
    {
        if (value is null ||
            !string.Equals(
                value,
                value.Trim(),
                StringComparison.Ordinal))
        {
            result = default;
            return false;
        }

        if (value.EndsWith(
                "Z",
                StringComparison.Ordinal))
        {
            return DateTimeOffset.TryParseExact(
                value,
                UniversalDateTimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out result);
        }

        return DateTimeOffset.TryParseExact(
            value,
            OffsetDateTimeFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    private static bool ScalarError(
        List<PolicyTestDiagnostic> errors,
        string path)
    {
        return AttributeError(
            errors,
            path,
            "Scalar value types require value and cannot define values.");
    }

    private static bool AttributeError(
        List<PolicyTestDiagnostic> errors,
        string path,
        string message)
    {
        AddError(
            errors,
            path,
            message);

        return false;
    }

    private static void AddError(
        List<PolicyTestDiagnostic> errors,
        string path,
        string message)
    {
        errors.Add(
            new PolicyTestDiagnostic(
                Category: "fixture",
                Code:
                    PolicyTestDiagnosticCodes
                        .InvalidFixture,
                Message: message,
                Path: path));
    }

    private static PolicyTestFixtureCompilation Failure(
        string code,
        string message)
    {
        return new PolicyTestFixtureCompilation(
            Suite: null,
            Errors:
            [
                new PolicyTestDiagnostic(
                    Category: "fixture",
                    Code: code,
                    Message: message)
            ]);
    }

    private static long? GetPosition(long value)
    {
        return value > 0
            ? value
            : null;
    }
}
