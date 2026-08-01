#!/usr/bin/env python3

from __future__ import annotations

from dataclasses import dataclass
from hashlib import sha256
from pathlib import Path
from tempfile import TemporaryDirectory
import gzip
import io
import json
import sys
import tarfile


EXPECTED_PACKAGES = {
    "@fotbiler/rulegate-client":
        "fotbiler-rulegate-client-{version}.tgz",
    "@fotbiler/rulegate-angular":
        "fotbiler-rulegate-angular-{version}.tgz",
    "@fotbiler/rulegate-angular-legacy":
        "fotbiler-rulegate-angular-legacy-{version}.tgz",
}

LIFECYCLE_SCRIPTS = {
    "preinstall",
    "install",
    "postinstall",
    "prepare",
    "prepack",
    "postpack",
    "prepublish",
    "prepublishOnly",
    "publish",
    "postpublish",
}

FORBIDDEN_PARTS = {
    "node_modules",
}

FORBIDDEN_BASENAMES = {
    ".npmrc",
    ".env",
    ".env.local",
    "package-lock.json",
    "pnpm-lock.yaml",
    "yarn.lock",
}

STATIC_FORBIDDEN_CONTENT = (
    b"NODE_AUTH_TOKEN",
    b"NPM_TOKEN",
    b"_authToken=",
)


@dataclass(frozen=True)
class Snapshot:
    raw_hash: str
    order: tuple[str, ...]
    payload: dict[str, tuple[object, ...]]
    metadata: dict[str, tuple[object, ...]]
    global_headers: tuple[tuple[str, str], ...]


def digest(value: bytes) -> str:
    return sha256(value).hexdigest()


def member_kind(
    member: tarfile.TarInfo,
) -> str:
    if member.isfile():
        return "file"

    if member.isdir():
        return "directory"

    if member.issym():
        return "symbolic-link"

    if member.islnk():
        return "hard-link"

    if member.ischr():
        return "character-device"

    if member.isblk():
        return "block-device"

    if member.isfifo():
        return "fifo"

    return f"other:{member.type!r}"


def snapshot(
    path: Path,
) -> Snapshot:
    raw = path.read_bytes()

    order: list[str] = []
    payload: dict[
        str,
        tuple[object, ...],
    ] = {}

    metadata: dict[
        str,
        tuple[object, ...],
    ] = {}

    with tarfile.open(
        path,
        mode="r:*",
    ) as archive:
        global_headers = tuple(
            sorted(
                (
                    str(key),
                    str(value),
                )
                for key, value
                in archive.pax_headers.items()
            )
        )

        for member in archive.getmembers():
            name = member.name

            if name in payload:
                raise RuntimeError(
                    f"Duplicate tar entry: {name}"
                )

            order.append(name)

            content = b""

            if member.isfile():
                stream = archive.extractfile(
                    member
                )

                if stream is None:
                    raise RuntimeError(
                        "Regular tar entry could not "
                        f"be read: {name}"
                    )

                content = stream.read()

            payload[name] = (
                member_kind(member),
                member.linkname,
                member.mode & 0o7777,
                len(content),
                digest(content),
            )

            metadata[name] = (
                member.uid,
                member.gid,
                member.uname,
                member.gname,
                member.mtime,
                tuple(
                    sorted(
                        (
                            str(key),
                            str(value),
                        )
                        for key, value
                        in member.pax_headers.items()
                    )
                ),
            )

    return Snapshot(
        raw_hash=digest(raw),
        order=tuple(order),
        payload=payload,
        metadata=metadata,
        global_headers=global_headers,
    )


def validate_package(
    path: Path,
    expected_name: str,
    expected_version: str,
) -> int:
    repository_root = (
        Path.cwd().resolve()
    ).as_posix().encode("utf-8")

    home_root = (
        Path.home().resolve()
    ).as_posix().encode("utf-8")

    forbidden_content = (
        *STATIC_FORBIDDEN_CONTENT,
        repository_root,
        home_root + b"/",
    )

    with tarfile.open(
        path,
        mode="r:*",
    ) as archive:
        members = archive.getmembers()

        names = {
            member.name
            for member in members
        }

        manifest_name = (
            "package/package.json"
        )

        if manifest_name not in names:
            raise RuntimeError(
                "Packed package manifest is missing: "
                f"{path}"
            )

        manifest_stream = archive.extractfile(
            archive.getmember(
                manifest_name
            )
        )

        if manifest_stream is None:
            raise RuntimeError(
                "Packed manifest could not be read: "
                f"{path}"
            )

        manifest = json.loads(
            manifest_stream.read().decode(
                "utf-8"
            )
        )

        if manifest.get(
            "name"
        ) != expected_name:
            raise RuntimeError(
                "Packed package name differs.\n"
                f"Expected: {expected_name}\n"
                f"Actual  : {manifest.get('name')!r}"
            )

        if manifest.get(
            "version"
        ) != expected_version:
            raise RuntimeError(
                "Packed package version differs.\n"
                f"Package : {expected_name}\n"
                f"Expected: {expected_version}\n"
                f"Actual  : {manifest.get('version')!r}"
            )

        if manifest.get(
            "private",
            False,
        ):
            raise RuntimeError(
                "Packed package is private: "
                f"{expected_name}"
            )

        publish_config = manifest.get(
            "publishConfig",
            {},
        )

        if publish_config.get(
            "access"
        ) != "public":
            raise RuntimeError(
                "Public npm access is missing: "
                f"{expected_name}"
            )

        if publish_config.get(
            "provenance"
        ) is not True:
            raise RuntimeError(
                "npm provenance is not enabled: "
                f"{expected_name}"
            )

        scripts = manifest.get(
            "scripts",
            {},
        ) or {}

        lifecycle = sorted(
            LIFECYCLE_SCRIPTS.intersection(
                scripts
            )
        )

        if lifecycle:
            raise RuntimeError(
                "Packed package contains lifecycle "
                f"scripts: {expected_name}: "
                f"{lifecycle!r}"
            )

        if manifest.get(
            "devDependencies"
        ) not in (
            None,
            {},
        ):
            raise RuntimeError(
                "Packed package contains "
                f"devDependencies: {expected_name}"
            )

        for member in members:
            path_value = Path(
                member.name
            )

            if set(
                path_value.parts
            ).intersection(
                FORBIDDEN_PARTS
            ):
                raise RuntimeError(
                    "Forbidden directory was packed: "
                    f"{member.name}"
                )

            if (
                path_value.name
                in FORBIDDEN_BASENAMES
            ):
                raise RuntimeError(
                    "Forbidden file was packed: "
                    f"{member.name}"
                )

            if member.issym() or member.islnk():
                raise RuntimeError(
                    "Package contains a link entry: "
                    f"{member.name}"
                )

            if (
                member.ischr()
                or member.isblk()
                or member.isfifo()
            ):
                raise RuntimeError(
                    "Package contains a special file: "
                    f"{member.name}"
                )

            if not member.isfile():
                continue

            stream = archive.extractfile(
                member
            )

            if stream is None:
                raise RuntimeError(
                    "Packed file could not be read: "
                    f"{member.name}"
                )

            content = stream.read()

            for token in forbidden_content:
                if token and token in content:
                    raise RuntimeError(
                        "Sensitive or machine-local "
                        "content was packed.\n"
                        f"Package: {expected_name}\n"
                        f"Entry  : {member.name}"
                    )

    return len(members)


def compare_runs(
    first_root: Path,
    second_root: Path,
    version: str,
) -> None:
    checked = 0
    raw_mismatches = 0
    payload_mismatches = 0
    metadata_mismatches = 0
    order_mismatches = 0

    for package_name in sorted(
        EXPECTED_PACKAGES
    ):
        filename = EXPECTED_PACKAGES[
            package_name
        ].format(
            version=version
        )

        first = first_root / filename
        second = second_root / filename

        if not first.is_file():
            raise RuntimeError(
                f"First artifact is missing: {first}"
            )

        if not second.is_file():
            raise RuntimeError(
                f"Second artifact is missing: {second}"
            )

        first_count = validate_package(
            first,
            package_name,
            version,
        )

        second_count = validate_package(
            second,
            package_name,
            version,
        )

        if first_count != second_count:
            raise RuntimeError(
                "Package entry counts differ: "
                f"{filename}"
            )

        left = snapshot(first)
        right = snapshot(second)

        checked += 1

        raw_equal = (
            left.raw_hash
            == right.raw_hash
        )

        payload_equal = (
            left.payload
            == right.payload
        )

        metadata_equal = (
            left.metadata
            == right.metadata
            and left.global_headers
            == right.global_headers
        )

        order_equal = (
            left.order
            == right.order
        )

        if not raw_equal:
            raw_mismatches += 1

        if not payload_equal:
            payload_mismatches += 1

        if not metadata_equal:
            metadata_mismatches += 1

        if not order_equal:
            order_mismatches += 1

        print()
        print(filename)
        print(
            "  first SHA-256      : "
            f"{left.raw_hash}"
        )

        print(
            "  second SHA-256     : "
            f"{right.raw_hash}"
        )

        print(
            "  entry count        : "
            f"{first_count}"
        )

        print(
            "  raw archive equal  : "
            f"{raw_equal}"
        )

        print(
            "  inventory equal    : "
            f"{set(left.payload) == set(right.payload)}"
        )

        print(
            "  entry order equal  : "
            f"{order_equal}"
        )

        print(
            "  payload equal      : "
            f"{payload_equal}"
        )

        print(
            "  metadata equal     : "
            f"{metadata_equal}"
        )

    print()
    print(
        f"npm artifacts checked       : {checked}"
    )

    print(
        f"Raw archive mismatches       : "
        f"{raw_mismatches}"
    )

    print(
        f"Meaningful payload mismatches: "
        f"{payload_mismatches}"
    )

    print(
        f"Metadata mismatches          : "
        f"{metadata_mismatches}"
    )

    print(
        f"Entry-order mismatches       : "
        f"{order_mismatches}"
    )

    if checked != 3:
        raise RuntimeError(
            "Expected exactly three npm artifacts."
        )

    if (
        raw_mismatches
        or payload_mismatches
        or metadata_mismatches
        or order_mismatches
    ):
        raise RuntimeError(
            "npm tarballs are not byte-for-byte "
            "reproducible."
        )

    print()
    print(
        "NPM_TARBALLS_BYTE_FOR_BYTE_"
        "REPRODUCIBLE"
    )

    print(
        "NPM_ARTIFACT_REPRODUCIBILITY_"
        "VERIFIED"
    )


def write_test_tar(
    path: Path,
    content: bytes,
    member_mtime: int,
    gzip_mtime: int,
) -> None:
    with path.open(
        "wb"
    ) as raw_stream:
        with gzip.GzipFile(
            filename="",
            mode="wb",
            fileobj=raw_stream,
            mtime=gzip_mtime,
        ) as gzip_stream:
            with tarfile.open(
                fileobj=gzip_stream,
                mode="w",
                format=tarfile.PAX_FORMAT,
            ) as archive:
                member = tarfile.TarInfo(
                    "package/value.txt"
                )

                member.size = len(content)
                member.mode = 0o644
                member.uid = 0
                member.gid = 0
                member.uname = ""
                member.gname = ""
                member.mtime = member_mtime

                archive.addfile(
                    member,
                    io.BytesIO(content),
                )


def run_self_test() -> None:
    with TemporaryDirectory() as value:
        root = Path(value)

        first = root / "first.tgz"
        metadata_changed = (
            root / "metadata-changed.tgz"
        )

        payload_changed = (
            root / "payload-changed.tgz"
        )

        write_test_tar(
            first,
            b"same-payload",
            member_mtime=1,
            gzip_mtime=1,
        )

        write_test_tar(
            metadata_changed,
            b"same-payload",
            member_mtime=2,
            gzip_mtime=2,
        )

        write_test_tar(
            payload_changed,
            b"changed-payload",
            member_mtime=1,
            gzip_mtime=1,
        )

        left = snapshot(first)

        metadata_snapshot = snapshot(
            metadata_changed
        )

        payload_snapshot = snapshot(
            payload_changed
        )

        if left.raw_hash == metadata_snapshot.raw_hash:
            raise RuntimeError(
                "Metadata mutation was not detected."
            )

        if left.payload != metadata_snapshot.payload:
            raise RuntimeError(
                "Metadata-only mutation changed "
                "the logical payload."
            )

        if left.metadata == metadata_snapshot.metadata:
            raise RuntimeError(
                "Member metadata mutation was not "
                "detected."
            )

        if left.payload == payload_snapshot.payload:
            raise RuntimeError(
                "Payload mutation was not detected."
            )

    print(
        "NPM_ARTIFACT_REPRODUCIBILITY_"
        "SELF_TEST_PASSED"
    )


def main() -> int:
    try:
        if sys.argv[1:] == [
            "--self-test"
        ]:
            run_self_test()
            return 0

        if len(sys.argv) != 4:
            print(
                "Usage: "
                "verify-npm-artifact-reproducibility.py "
                "--self-test | "
                "<run-one> <run-two> <version>",
                file=sys.stderr,
            )

            return 2

        compare_runs(
            Path(sys.argv[1]),
            Path(sys.argv[2]),
            sys.argv[3],
        )

        return 0
    except (
        OSError,
        RuntimeError,
        tarfile.TarError,
        json.JSONDecodeError,
    ) as exception:
        print(
            f"ERROR: {exception}",
            file=sys.stderr,
        )

        return 1


if __name__ == "__main__":
    raise SystemExit(main())
