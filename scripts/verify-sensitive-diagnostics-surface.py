#!/usr/bin/env python3

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import re
import sys


REPO_ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = REPO_ROOT / "src"

ALLOWED_DIAGNOSTICS_SINK_FILES = {
    (
        "src/Fotbiler.RuleGate.Core/"
        "Diagnostics/RuleGateInstrumentation.cs"
    ),
    (
        "src/Fotbiler.RuleGate.AspNetCore/"
        "Diagnostics/"
        "LoggingAuthorizationDiagnosticsSink.cs"
    ),
    (
        "src/Fotbiler.RuleGate.AspNetCore/"
        "Diagnostics/"
        "LoggingRuleGateEnrichmentDiagnosticsSink.cs"
    ),
    (
        "src/Fotbiler.RuleGate.AspNetCore/"
        "PolicySources/"
        "PolicySourceReloadHostedService.cs"
    ),
}

LOGGING_FILES = {
    (
        "src/Fotbiler.RuleGate.AspNetCore/"
        "Diagnostics/"
        "LoggingAuthorizationDiagnosticsSink.cs"
    ),
    (
        "src/Fotbiler.RuleGate.AspNetCore/"
        "Diagnostics/"
        "LoggingRuleGateEnrichmentDiagnosticsSink.cs"
    ),
    (
        "src/Fotbiler.RuleGate.AspNetCore/"
        "PolicySources/"
        "PolicySourceReloadHostedService.cs"
    ),
}

INSTRUMENTATION_FILE = (
    "src/Fotbiler.RuleGate.Core/"
    "Diagnostics/RuleGateInstrumentation.cs"
)

SINK_MARKER = re.compile(
    r"\bILogger\s*<|"
    r"\.(?:LogTrace|LogDebug|LogInformation|"
    r"LogWarning|LogError|LogCritical)\s*\(|"
    r"\bActivitySource\b|"
    r"\bDiagnosticSource\b|"
    r"\bDiagnosticListener\b|"
    r"\bCreateCounter\s*<|"
    r"\bCreateHistogram\s*<|"
    r"\bSetTag\s*\(|"
    r"\bAddTag\s*\(|"
    r"\bAddEvent\s*\(|"
    r"\bConsole\.(?:Write|WriteLine)\s*\("
)

LOG_CALL_START = re.compile(
    r"\b(?:logger|_logger)"
    r"\.(?:LogTrace|LogDebug|LogInformation|"
    r"LogWarning|LogError|LogCritical)\s*\("
)

NORMAL_STRING = re.compile(
    r'(?:\$@|@\\$|@|\$)?"'
    r'(?:\\.|""|[^"\\])*"',
    re.DOTALL,
)

PLACEHOLDER = re.compile(
    r"\{"
    r"(?P<name>[A-Za-z_][A-Za-z0-9_.]*)"
    r"(?:[^}]*)"
    r"\}"
)

FORBIDDEN_PLACEHOLDER = re.compile(
    r"^(?:"
    r"Subject|SubjectId|"
    r"Resource|ResourceId|ResourceType|"
    r"Principal|"
    r"Claim|Claims|"
    r"Role|Roles|"
    r"Permission|Permissions|"
    r"AttributeName|ComparedAttributeName|"
    r"AttributeValue|ComparedAttributeValue|"
    r"ExpectedValue|ActualValue|"
    r"AccessToken|RefreshToken|Token|"
    r"ClientSecret|Secret|Password|"
    r"AuthorizationHeader|Header|Headers|"
    r"Cookie|Cookies|"
    r"RequestBody|QueryString|"
    r"Exception|ExceptionMessage|"
    r"StackTrace|InnerException|"
    r"ManifestContent|Yaml"
    r")$",
    re.IGNORECASE,
)

FORBIDDEN_IDENTIFIER = re.compile(
    r"\b(?:"
    r"subject|subjectId|"
    r"resource|resourceId|resourceType|"
    r"principal|"
    r"claim|claims|"
    r"role|roles|"
    r"permission|permissions|"
    r"attributeName|comparedAttributeName|"
    r"attributeValue|comparedAttributeValue|"
    r"expectedValue|actualValue|"
    r"accessToken|refreshToken|token|"
    r"clientSecret|secret|password|"
    r"authorizationHeader|header|headers|"
    r"cookie|cookies|"
    r"requestBody|queryString|"
    r"exception|stackTrace|innerException|"
    r"manifestContent|yaml"
    r")\b",
    re.IGNORECASE,
)

FORBIDDEN_TELEMETRY_KEY = re.compile(
    r"(?:^|[._-])(?:"
    r"subject|"
    r"resource|"
    r"claim|"
    r"role|"
    r"permission|"
    r"token|"
    r"secret|"
    r"password|"
    r"header|"
    r"cookie|"
    r"request[_-]?body|"
    r"query[_-]?string|"
    r"exception|"
    r"stack|"
    r"attribute(?:[._-](?:name|value))?"
    r")(?:$|[._-])",
    re.IGNORECASE,
)


@dataclass(frozen=True)
class Finding:
    path: str
    line: int
    message: str


def relative(path: Path) -> str:
    return path.relative_to(
        REPO_ROOT
    ).as_posix()


def source_files() -> list[Path]:
    return sorted(
        path
        for path in SOURCE_ROOT.rglob("*.cs")
        if "bin" not in path.parts
        and "obj" not in path.parts
    )


def find_matching_parenthesis(
    text: str,
    opening_index: int,
) -> int:
    depth = 0
    index = opening_index
    state = "code"

    while index < len(text):
        character = text[index]
        following = (
            text[index + 1]
            if index + 1 < len(text)
            else ""
        )

        if state == "line-comment":
            if character == "\n":
                state = "code"

            index += 1
            continue

        if state == "block-comment":
            if character == "*" and following == "/":
                state = "code"
                index += 2
                continue

            index += 1
            continue

        if state == "normal-string":
            if character == "\\":
                index += 2
                continue

            if character == '"':
                state = "code"

            index += 1
            continue

        if state == "verbatim-string":
            if character == '"' and following == '"':
                index += 2
                continue

            if character == '"':
                state = "code"

            index += 1
            continue

        if state == "character":
            if character == "\\":
                index += 2
                continue

            if character == "'":
                state = "code"

            index += 1
            continue

        if character == "/" and following == "/":
            state = "line-comment"
            index += 2
            continue

        if character == "/" and following == "*":
            state = "block-comment"
            index += 2
            continue

        if character == "@" and following == '"':
            state = "verbatim-string"
            index += 2
            continue

        if character == '"':
            state = "normal-string"
            index += 1
            continue

        if character == "'":
            state = "character"
            index += 1
            continue

        if character == "(":
            depth += 1

        elif character == ")":
            depth -= 1

            if depth == 0:
                return index

        index += 1

    raise RuntimeError(
        "Unbalanced diagnostics sink invocation."
    )


def extract_log_calls(
    text: str,
) -> list[tuple[int, str]]:
    result: list[tuple[int, str]] = []

    for match in LOG_CALL_START.finditer(
        text
    ):
        opening = text.find(
            "(",
            match.start(),
        )

        if opening < 0:
            raise RuntimeError(
                "Logging invocation opening "
                "parenthesis was not found."
            )

        closing = find_matching_parenthesis(
            text,
            opening,
        )

        result.append(
            (
                match.start(),
                text[
                    match.start():
                    closing + 1
                ],
            )
        )

    return result


def string_values(
    text: str,
) -> list[str]:
    values: list[str] = []

    for match in NORMAL_STRING.finditer(
        text
    ):
        literal = match.group(0)
        opening = literal.find('"')

        if opening < 0:
            continue

        body = literal[
            opening + 1:
            -1
        ]

        values.append(body)

    return values


def strip_strings_and_comments(
    text: str,
) -> str:
    output = list(text)
    index = 0
    state = "code"

    while index < len(text):
        character = text[index]
        following = (
            text[index + 1]
            if index + 1 < len(text)
            else ""
        )

        if state == "line-comment":
            if character == "\n":
                state = "code"
            else:
                output[index] = " "

            index += 1
            continue

        if state == "block-comment":
            output[index] = " "

            if character == "*" and following == "/":
                output[index + 1] = " "
                state = "code"
                index += 2
                continue

            index += 1
            continue

        if state == "normal-string":
            output[index] = " "

            if character == "\\":
                if index + 1 < len(output):
                    output[index + 1] = " "

                index += 2
                continue

            if character == '"':
                state = "code"

            index += 1
            continue

        if state == "verbatim-string":
            output[index] = " "

            if character == '"' and following == '"':
                output[index + 1] = " "
                index += 2
                continue

            if character == '"':
                state = "code"

            index += 1
            continue

        if state == "character":
            output[index] = " "

            if character == "\\":
                if index + 1 < len(output):
                    output[index + 1] = " "

                index += 2
                continue

            if character == "'":
                state = "code"

            index += 1
            continue

        if character == "/" and following == "/":
            output[index] = " "
            output[index + 1] = " "
            state = "line-comment"
            index += 2
            continue

        if character == "/" and following == "*":
            output[index] = " "
            output[index + 1] = " "
            state = "block-comment"
            index += 2
            continue

        if character == "@" and following == '"':
            output[index] = " "
            output[index + 1] = " "
            state = "verbatim-string"
            index += 2
            continue

        if character == '"':
            output[index] = " "
            state = "normal-string"
            index += 1
            continue

        if character == "'":
            output[index] = " "
            state = "character"
            index += 1
            continue

        index += 1

    return "".join(output)


def analyze_log_call(
    path: str,
    line: int,
    call: str,
) -> list[Finding]:
    findings: list[Finding] = []

    for value in string_values(
        call
    ):
        for placeholder_match in PLACEHOLDER.finditer(
            value
        ):
            placeholder = placeholder_match.group(
                "name"
            )

            if FORBIDDEN_PLACEHOLDER.fullmatch(
                placeholder
            ):
                findings.append(
                    Finding(
                        path,
                        line,
                        (
                            "Forbidden sensitive log "
                            "placeholder: "
                            f"{placeholder}"
                        ),
                    )
                )

    code_only = strip_strings_and_comments(
        call
    )

    for match in FORBIDDEN_IDENTIFIER.finditer(
        code_only
    ):
        findings.append(
            Finding(
                path,
                line,
                (
                    "Sensitive identifier passed "
                    "to logging sink: "
                    f"{match.group(0)}"
                ),
            )
        )

    return findings


def discover_sink_files() -> set[str]:
    discovered: set[str] = set()

    for path in source_files():
        text = path.read_text(
            encoding="utf-8"
        )

        if SINK_MARKER.search(text):
            discovered.add(
                relative(path)
            )

    return discovered


def verify_sink_inventory() -> list[Finding]:
    discovered = discover_sink_files()

    unexpected = sorted(
        discovered
        - ALLOWED_DIAGNOSTICS_SINK_FILES
    )

    missing = sorted(
        ALLOWED_DIAGNOSTICS_SINK_FILES
        - discovered
    )

    findings: list[Finding] = []

    for path in unexpected:
        findings.append(
            Finding(
                path,
                1,
                (
                    "New production diagnostics sink "
                    "requires explicit privacy review."
                ),
            )
        )

    for path in missing:
        findings.append(
            Finding(
                path,
                1,
                (
                    "Approved diagnostics sink "
                    "inventory entry is missing."
                ),
            )
        )

    return findings


def verify_logging_calls() -> list[Finding]:
    findings: list[Finding] = []

    for relative_path in sorted(
        LOGGING_FILES
    ):
        path = REPO_ROOT / relative_path

        if not path.is_file():
            findings.append(
                Finding(
                    relative_path,
                    1,
                    "Logging source file is missing.",
                )
            )

            continue

        text = path.read_text(
            encoding="utf-8"
        )

        calls = extract_log_calls(
            text
        )

        if not calls:
            findings.append(
                Finding(
                    relative_path,
                    1,
                    (
                        "Expected logging calls "
                        "were not found."
                    ),
                )
            )

            continue

        for start, call in calls:
            line = (
                text.count(
                    "\n",
                    0,
                    start,
                )
                + 1
            )

            findings.extend(
                analyze_log_call(
                    relative_path,
                    line,
                    call,
                )
            )

    return findings


def verify_telemetry_keys() -> list[Finding]:
    path = REPO_ROOT / INSTRUMENTATION_FILE

    if not path.is_file():
        return [
            Finding(
                INSTRUMENTATION_FILE,
                1,
                "Instrumentation source file is missing.",
            )
        ]

    text = path.read_text(
        encoding="utf-8"
    )

    findings: list[Finding] = []
    key_count = 0

    for match in NORMAL_STRING.finditer(
        text
    ):
        literal = match.group(0)
        opening = literal.find('"')

        if opening < 0:
            continue

        value = literal[
            opening + 1:
            -1
        ]

        if not value.startswith(
            "rulegate."
        ):
            continue

        key_count += 1

        if FORBIDDEN_TELEMETRY_KEY.search(
            value
        ):
            line = (
                text.count(
                    "\n",
                    0,
                    match.start(),
                )
                + 1
            )

            findings.append(
                Finding(
                    INSTRUMENTATION_FILE,
                    line,
                    (
                        "Sensitive or high-cardinality "
                        "telemetry key: "
                        f"{value}"
                    ),
                )
            )

    if key_count == 0:
        findings.append(
            Finding(
                INSTRUMENTATION_FILE,
                1,
                (
                    "No RuleGate telemetry vocabulary "
                    "was discovered."
                ),
            )
        )

    return findings


def verify_privacy_test_sentinels() -> list[Finding]:
    requirements = {
        (
            "tests/Fotbiler.RuleGate.Core.Tests/"
            "RuleGateTelemetryTests.cs"
        ): (
            "AssertTelemetryDoesNotContain",
            "secret-subject-id",
            "secret-resource-id",
            "secret-resource-type",
            "sensitive.permission",
            "secret-policy-id",
            "secret-source-name",
        ),
        (
            "tests/Fotbiler.RuleGate.AspNetCore.Tests/"
            "RuleGateLoggingDiagnosticsTests.cs"
        ): (
            "sensitiveAttributeName",
            "sensitiveComparedAttributeName",
            "sensitiveAttributeValue",
            "ultra-secret",
            "sensitiveExceptionMessage",
            "database-secret-in-exception",
        ),
    }

    findings: list[Finding] = []

    for relative_path, sentinels in requirements.items():
        path = REPO_ROOT / relative_path

        if not path.is_file():
            findings.append(
                Finding(
                    relative_path,
                    1,
                    "Privacy regression test file is missing.",
                )
            )

            continue

        text = path.read_text(
            encoding="utf-8"
        )

        for sentinel in sentinels:
            if sentinel not in text:
                findings.append(
                    Finding(
                        relative_path,
                        1,
                        (
                            "Privacy regression sentinel "
                            f"is missing: {sentinel}"
                        ),
                    )
                )

    return findings


def verify_keycloak_has_no_sink() -> list[Finding]:
    root = (
        SOURCE_ROOT
        / "Fotbiler.RuleGate.Keycloak"
    )

    findings: list[Finding] = []

    for path in sorted(
        root.rglob("*.cs")
    ):
        if "bin" in path.parts or "obj" in path.parts:
            continue

        text = path.read_text(
            encoding="utf-8"
        )

        if not SINK_MARKER.search(text):
            continue

        findings.append(
            Finding(
                relative(path),
                1,
                (
                    "Keycloak integration introduced "
                    "a diagnostics sink requiring an "
                    "explicit sensitive-data review."
                ),
            )
        )

    return findings


def run_repository_check() -> None:
    findings = [
        *verify_sink_inventory(),
        *verify_logging_calls(),
        *verify_telemetry_keys(),
        *verify_privacy_test_sentinels(),
        *verify_keycloak_has_no_sink(),
    ]

    if findings:
        for finding in findings:
            print(
                f"{finding.path}:{finding.line}: "
                f"{finding.message}",
                file=sys.stderr,
            )

        raise RuntimeError(
            "Sensitive diagnostics regression "
            "verification failed."
        )

    print(
        "Approved production diagnostics sinks : "
        f"{len(ALLOWED_DIAGNOSTICS_SINK_FILES)}"
    )

    print(
        "Logging source files reviewed         : "
        f"{len(LOGGING_FILES)}"
    )

    print(
        "Privacy regression test files         : 2"
    )

    print(
        "Keycloak diagnostics sinks            : 0"
    )

    print(
        "SENSITIVE_DIAGNOSTICS_SURFACE_VERIFIED"
    )


def run_self_test() -> None:
    safe_call = """
        logger.LogInformation(
            "RuleGate authorization evaluation "
            "{EvaluationId} completed. "
            "PolicyId: {PolicyId}; "
            "FailureCodes: {FailureCodes}.",
            evaluationId,
            policyId,
            failureCodes);
    """

    unsafe_placeholder = """
        logger.LogInformation(
            "Subject: {SubjectId}",
            diagnostic.SubjectId);
    """

    unsafe_value = """
        logger.LogDebug(
            "Value recorded.",
            diagnostic.AttributeValue);
    """

    unsafe_exception = """
        logger.LogWarning(
            exception,
            "Provider failed.");
    """

    if analyze_log_call(
        "self-test.cs",
        1,
        safe_call,
    ):
        raise RuntimeError(
            "Safe diagnostics self-test was rejected."
        )

    for value in (
        unsafe_placeholder,
        unsafe_value,
        unsafe_exception,
    ):
        if not analyze_log_call(
            "self-test.cs",
            1,
            value,
        ):
            raise RuntimeError(
                "Unsafe diagnostics self-test "
                "was not rejected."
            )

    if FORBIDDEN_TELEMETRY_KEY.search(
        "rulegate.authorization.outcome"
    ):
        raise RuntimeError(
            "Safe telemetry key self-test "
            "was rejected."
        )

    if not FORBIDDEN_TELEMETRY_KEY.search(
        "rulegate.subject.id"
    ):
        raise RuntimeError(
            "Sensitive telemetry key self-test "
            "was not rejected."
        )

    print(
        "SENSITIVE_DIAGNOSTICS_VERIFIER_"
        "SELF_TEST_PASSED"
    )


def main() -> int:
    try:
        if sys.argv[1:] == [
            "--self-test"
        ]:
            run_self_test()
            return 0

        if sys.argv[1:] == [
            "--check"
        ]:
            run_repository_check()
            return 0

        print(
            "Usage: "
            "verify-sensitive-diagnostics-surface.py "
            "--self-test | --check",
            file=sys.stderr,
        )

        return 2
    except RuntimeError as exception:
        print(
            f"ERROR: {exception}",
            file=sys.stderr,
        )

        return 1


if __name__ == "__main__":
    raise SystemExit(main())
