#!/usr/bin/env python3

from __future__ import annotations

from dataclasses import dataclass
from hashlib import sha256
from pathlib import Path
from tempfile import TemporaryDirectory
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo
import re
import sys
import xml.etree.ElementTree as ET


CORE_PATTERN = re.compile(
    r"^package/services/metadata/"
    r"core-properties/"
    r"[^/]+\.psmdcp$"
)

CORE_LOGICAL_NAME = (
    "package/services/metadata/"
    "core-properties/"
    "__normalized__.psmdcp"
)

RELATIONSHIPS_NAME = "_rels/.rels"

CORE_RELATIONSHIP_TYPE = (
    "http://schemas.openxmlformats.org/"
    "package/2006/relationships/"
    "metadata/core-properties"
)

VOLATILE_CORE_ELEMENTS = {
    "created",
    "modified",
}


class ComparisonError(RuntimeError):
    pass


@dataclass(frozen=True)
class ComparisonResult:
    filename: str
    byte_identical: bool
    metadata_difference_count: int


def digest(data: bytes) -> str:
    return sha256(data).hexdigest()


def file_digest(path: Path) -> str:
    return digest(path.read_bytes())


def local_name(tag: str) -> str:
    if "}" in tag:
        return tag.rsplit("}", 1)[1]

    return tag


def normalize_element(
    element: ET.Element,
    *,
    normalize_core_properties: bool,
) -> None:
    if (
        normalize_core_properties
        and local_name(element.tag)
        in VOLATILE_CORE_ELEMENTS
    ):
        element.text = "__NORMALIZED_TIMESTAMP__"

    attributes = sorted(
        element.attrib.items(),
        key=lambda item: item[0],
    )

    element.attrib.clear()

    for key, value in attributes:
        element.attrib[key] = value

    for child in list(element):
        normalize_element(
            child,
            normalize_core_properties=
                normalize_core_properties,
        )

    children = list(element)

    children.sort(
        key=lambda child: (
            child.tag,
            tuple(sorted(child.attrib.items())),
            child.text or "",
            ET.tostring(
                child,
                encoding="unicode",
            ),
        )
    )

    element[:] = children


def canonicalize_core_properties(
    data: bytes,
) -> bytes:
    root = ET.fromstring(data)

    normalize_element(
        root,
        normalize_core_properties=True,
    )

    return ET.tostring(
        root,
        encoding="utf-8",
    )


def canonicalize_relationships(
    data: bytes,
) -> bytes:
    root = ET.fromstring(data)

    core_relationship_count = 0

    for relationship in list(root):
        if (
            relationship.attrib.get("Type")
            != CORE_RELATIONSHIP_TYPE
        ):
            continue

        core_relationship_count += 1

        relationship.attrib["Id"] = (
            "__CORE_PROPERTIES_RELATIONSHIP__"
        )

        relationship.attrib["Target"] = (
            CORE_LOGICAL_NAME
        )

    if core_relationship_count != 1:
        raise ComparisonError(
            "Expected exactly one NuGet "
            "core-properties relationship, found "
            f"{core_relationship_count}."
        )

    normalize_element(
        root,
        normalize_core_properties=False,
    )

    return ET.tostring(
        root,
        encoding="utf-8",
    )


def logical_name(name: str) -> str:
    if CORE_PATTERN.fullmatch(name):
        return CORE_LOGICAL_NAME

    return name


def build_logical_map(
    archive: ZipFile,
) -> dict[str, ZipInfo]:
    result: dict[str, ZipInfo] = {}

    core_entries = [
        info
        for info in archive.infolist()
        if CORE_PATTERN.fullmatch(
            info.filename
        )
    ]

    if len(core_entries) != 1:
        raise ComparisonError(
            "Expected exactly one generated "
            "core-properties entry, found "
            f"{len(core_entries)}."
        )

    for info in archive.infolist():
        logical = logical_name(
            info.filename
        )

        if logical in result:
            raise ComparisonError(
                "Logical ZIP entry collision: "
                f"{logical}"
            )

        result[logical] = info

    return result


def canonical_content(
    archive: ZipFile,
    info: ZipInfo,
) -> bytes:
    data = archive.read(
        info.filename
    )

    logical = logical_name(
        info.filename
    )

    if logical == CORE_LOGICAL_NAME:
        return canonicalize_core_properties(
            data
        )

    if logical == RELATIONSHIPS_NAME:
        return canonicalize_relationships(
            data
        )

    return data


def metadata_tuple(
    info: ZipInfo,
) -> tuple[object, ...]:
    return (
        info.date_time,
        info.compress_type,
        info.extra,
        info.comment,
        info.external_attr,
        info.internal_attr,
        info.create_system,
        info.create_version,
        info.extract_version,
        info.flag_bits,
    )


def compare_pair(
    first_path: Path,
    second_path: Path,
) -> ComparisonResult:
    if not first_path.is_file():
        raise ComparisonError(
            f"Missing first artifact: {first_path}"
        )

    if not second_path.is_file():
        raise ComparisonError(
            f"Missing second artifact: {second_path}"
        )

    first_archive_hash = file_digest(
        first_path
    )

    second_archive_hash = file_digest(
        second_path
    )

    byte_identical = (
        first_archive_hash
        == second_archive_hash
    )

    with (
        ZipFile(first_path) as first_zip,
        ZipFile(second_path) as second_zip,
    ):
        first_map = build_logical_map(
            first_zip
        )

        second_map = build_logical_map(
            second_zip
        )

        first_names = set(first_map)
        second_names = set(second_map)

        if first_names != second_names:
            only_first = sorted(
                first_names - second_names
            )

            only_second = sorted(
                second_names - first_names
            )

            raise ComparisonError(
                "Normalized logical inventory differs "
                f"for {first_path.name}.\n"
                f"Only first: {only_first!r}\n"
                f"Only second: {only_second!r}"
            )

        payload_differences = []

        for logical in sorted(
            first_names
        ):
            first_content = canonical_content(
                first_zip,
                first_map[logical],
            )

            second_content = canonical_content(
                second_zip,
                second_map[logical],
            )

            if first_content == second_content:
                continue

            payload_differences.append(
                (
                    logical,
                    digest(first_content),
                    digest(second_content),
                )
            )

        if payload_differences:
            lines = [
                (
                    "Meaningful NuGet package payload "
                    f"differs for {first_path.name}."
                )
            ]

            for (
                logical,
                first_hash,
                second_hash,
            ) in payload_differences:
                lines.extend(
                    [
                        f"Entry: {logical}",
                        f"Run one: {first_hash}",
                        f"Run two: {second_hash}",
                    ]
                )

            raise ComparisonError(
                "\n".join(lines)
            )

        metadata_difference_count = 0

        for logical in sorted(
            first_names
        ):
            if metadata_tuple(
                first_map[logical]
            ) != metadata_tuple(
                second_map[logical]
            ):
                metadata_difference_count += 1

    return ComparisonResult(
        filename=first_path.name,
        byte_identical=byte_identical,
        metadata_difference_count=
            metadata_difference_count,
    )


def compare_directories(
    run_one: Path,
    run_two: Path,
    version: str,
    package_ids: list[str],
) -> None:
    expected_names = {
        f"{package_id}.{version}.{extension}"
        for package_id in package_ids
        for extension in (
            "nupkg",
            "snupkg",
        )
    }

    first_names = {
        path.name
        for path in run_one.iterdir()
        if path.is_file()
    }

    second_names = {
        path.name
        for path in run_two.iterdir()
        if path.is_file()
    }

    if first_names != expected_names:
        raise ComparisonError(
            "First build artifact inventory differs "
            "from the expected 12 NuGet artifacts.\n"
            f"Expected: {sorted(expected_names)!r}\n"
            f"Actual: {sorted(first_names)!r}"
        )

    if second_names != expected_names:
        raise ComparisonError(
            "Second build artifact inventory differs "
            "from the expected 12 NuGet artifacts.\n"
            f"Expected: {sorted(expected_names)!r}\n"
            f"Actual: {sorted(second_names)!r}"
        )

    results = [
        compare_pair(
            run_one / filename,
            run_two / filename,
        )
        for filename in sorted(
            expected_names
        )
    ]

    byte_identical_count = sum(
        result.byte_identical
        for result in results
    )

    normalized_only_count = (
        len(results)
        - byte_identical_count
    )

    metadata_difference_count = sum(
        result.metadata_difference_count
        for result in results
    )

    for result in results:
        classification = (
            "BYTE_IDENTICAL"
            if result.byte_identical
            else
            "CONTAINER_METADATA_ONLY_DIFFERENCE"
        )

        print(
            f"{result.filename}"
            f"|{classification}"
            f"|metadataDifferences="
            f"{result.metadata_difference_count}"
        )

    print()
    print(
        "NuGet artifacts checked       : "
        f"{len(results)}"
    )

    print(
        "Byte-identical artifacts      : "
        f"{byte_identical_count}"
    )

    print(
        "Normalized-only artifacts     : "
        f"{normalized_only_count}"
    )

    print(
        "ZIP metadata differences      : "
        f"{metadata_difference_count}"
    )

    print(
        "Meaningful payload mismatches : 0"
    )

    print(
        "NUGET_PAYLOAD_REPRODUCIBILITY_VERIFIED"
    )


def write_synthetic_package(
    path: Path,
    *,
    core_identifier: str,
    relationship_identifier: str,
    created: str,
    modified: str,
    payload: bytes,
) -> None:
    core_path = (
        "package/services/metadata/"
        "core-properties/"
        f"{core_identifier}.psmdcp"
    )

    relationships = f"""\
<?xml version="1.0" encoding="utf-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship
    Type="{CORE_RELATIONSHIP_TYPE}"
    Target="{core_path}"
    Id="{relationship_identifier}" />
</Relationships>
"""

    core_properties = f"""\
<?xml version="1.0" encoding="utf-8"?>
<coreProperties
  xmlns="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"
  xmlns:dcterms="http://purl.org/dc/terms/">
  <dcterms:created>{created}</dcterms:created>
  <dcterms:modified>{modified}</dcterms:modified>
</coreProperties>
"""

    with ZipFile(
        path,
        "w",
        compression=ZIP_DEFLATED,
    ) as archive:
        archive.writestr(
            RELATIONSHIPS_NAME,
            relationships,
        )

        archive.writestr(
            core_path,
            core_properties,
        )

        archive.writestr(
            "lib/net8.0/Test.dll",
            payload,
        )

        archive.writestr(
            "Test.nuspec",
            b"<package />",
        )


def run_self_test() -> None:
    with TemporaryDirectory() as directory:
        root = Path(directory)

        first = root / "first.nupkg"
        second = root / "second.nupkg"

        write_synthetic_package(
            first,
            core_identifier="first-guid",
            relationship_identifier="first-relation",
            created="2026-01-01T00:00:00Z",
            modified="2026-01-01T00:00:00Z",
            payload=b"identical-payload",
        )

        write_synthetic_package(
            second,
            core_identifier="second-guid",
            relationship_identifier="second-relation",
            created="2026-02-01T00:00:00Z",
            modified="2026-02-01T00:00:00Z",
            payload=b"identical-payload",
        )

        successful_result = compare_pair(
            first,
            second,
        )

        if successful_result.byte_identical:
            raise ComparisonError(
                "Self-test expected different "
                "container bytes."
            )

        write_synthetic_package(
            second,
            core_identifier="third-guid",
            relationship_identifier="third-relation",
            created="2026-03-01T00:00:00Z",
            modified="2026-03-01T00:00:00Z",
            payload=b"mutated-payload",
        )

        mutation_was_rejected = False

        try:
            compare_pair(
                first,
                second,
            )
        except ComparisonError:
            mutation_was_rejected = True

        if not mutation_was_rejected:
            raise ComparisonError(
                "Self-test meaningful payload "
                "mutation was not rejected."
            )

    print(
        "NUGET_PAYLOAD_REPRODUCIBILITY_SELF_TEST_PASSED"
    )


def main() -> int:
    try:
        if sys.argv[1:] == [
            "--self-test"
        ]:
            run_self_test()
            return 0

        if len(sys.argv) < 5:
            print(
                "Usage: "
                "verify-nuget-payload-reproducibility.py "
                "<run-one> <run-two> <version> "
                "<package-id>...",
                file=sys.stderr,
            )

            return 2

        run_one = Path(sys.argv[1])
        run_two = Path(sys.argv[2])
        version = sys.argv[3]
        package_ids = sys.argv[4:]

        compare_directories(
            run_one,
            run_two,
            version,
            package_ids,
        )

        return 0
    except (
        ComparisonError,
        ET.ParseError,
    ) as exception:
        print(
            f"ERROR: {exception}",
            file=sys.stderr,
        )

        return 1


if __name__ == "__main__":
    raise SystemExit(main())
