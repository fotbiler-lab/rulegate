#!/usr/bin/env node

import { readFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, '..');
const errors = [];

const buildProps = await readRepositoryFile('Directory.Build.props');
const nugetVersion = readXmlValue(buildProps, 'VersionPrefix');
const nugetSuffix = readXmlValue(buildProps, 'VersionSuffix');

if (!/^\d+\.\d+\.\d+$/u.test(nugetVersion)) {
  errors.push(
    `Directory.Build.props: VersionPrefix '${nugetVersion}' is not a stable semantic version.`,
  );
}

const npmPackageFiles = [
  'src/Fotbiler.RuleGate.Client/package.json',
  'src/Fotbiler.RuleGate.Angular/package.json',
  'src/Fotbiler.RuleGate.Angular.Legacy/package.json',
];
const npmPackages = await Promise.all(
  npmPackageFiles.map(async (file) => ({
    file,
    manifest: JSON.parse(await readRepositoryFile(file)),
  })),
);
const npmVersions = new Set(npmPackages.map(({ manifest }) => manifest.version));

if (npmVersions.size !== 1) {
  errors.push(
    `npm package family is not aligned: ${npmPackages
      .map(({ file, manifest }) => `${file}=${manifest.version}`)
      .join(', ')}`,
  );
}

const npmVersion = npmPackages[0].manifest.version;
if (!/^\d+\.\d+\.\d+$/u.test(npmVersion)) {
  errors.push(`npm package version '${npmVersion}' is not a stable semantic version.`);
}

const stableRelease = nugetSuffix.length === 0 && !npmVersion.includes('-');
if (stableRelease) {
  await verifyStableDocumentation();
}

if (errors.length > 0) {
  console.error(`Release-state verification failed with ${errors.length} error(s):`);

  for (const error of errors) {
    console.error(`- ${error}`);
  }

  process.exit(1);
}

console.log(
  `Release-state verification passed: NuGet ${formatVersion(nugetVersion, nugetSuffix)}, npm ${npmVersion}, ${
    stableRelease ? 'stable documentation checked' : 'prerelease documentation check skipped'
  }.`,
);

async function verifyStableDocumentation() {
  const publicReleaseFiles = [
    'README.md',
    'SECURITY.md',
    'SUPPORT.md',
    'docs/README.md',
    'docs/guide/README.md',
  ];
  const staleCurrentStatePatterns = [
    /\bcurrently (?:an?\s+)?(?:preview|prerelease|pre-release|release candidate)\b/giu,
    /\bcurrent (?:project|release|package line) is (?:an?\s+)?(?:preview|prerelease|pre-release|release candidate)\b/giu,
    /\blatest (?:release|package line) is (?:an?\s+)?(?:preview|prerelease|pre-release|release candidate)\b/giu,
  ];

  for (const file of publicReleaseFiles) {
    const content = await readRepositoryFile(file);

    for (const pattern of staleCurrentStatePatterns) {
      for (const match of content.matchAll(pattern)) {
        errors.push(`${file}: stale current-release wording '${match[0]}'.`);
      }
    }
  }

  const support = await readRepositoryFile('SUPPORT.md');
  requireText(
    'SUPPORT.md',
    support,
    /\bstable open-source project\b/iu,
    'must describe RuleGate as a stable open-source project',
  );
  requireText(
    'SUPPORT.md',
    support,
    /\bdoes not provide paid or commercial support\b/iu,
    'must state the community-only support model',
  );

  const readme = await readRepositoryFile('README.md');
  requireText(
    'README.md',
    readme,
    new RegExp(`--version\\s+${escapeRegExp(nugetVersion)}\\b`, 'u'),
    `must install the current NuGet version ${nugetVersion}`,
  );

  const security = await readRepositoryFile('SECURITY.md');
  requireText(
    'SECURITY.md',
    security,
    new RegExp(`\\b${escapeRegExp(nugetVersion)}\\b`, 'u'),
    `must name the supported NuGet version ${nugetVersion}`,
  );
  requireText(
    'SECURITY.md',
    security,
    new RegExp(`\\b${escapeRegExp(npmVersion)}\\b`, 'u'),
    `must name the supported npm version ${npmVersion}`,
  );
}

function requireText(file, content, pattern, message) {
  if (!pattern.test(content)) {
    errors.push(`${file}: ${message}.`);
  }
}

function readXmlValue(xml, element) {
  const match = new RegExp(`<${element}>([^<]*)</${element}>`, 'u').exec(xml);

  if (!match) {
    errors.push(`Directory.Build.props: missing <${element}>.`);
    return '';
  }

  return match[1].trim();
}

async function readRepositoryFile(relativePath) {
  return readFile(path.join(repositoryRoot, relativePath), 'utf8');
}

function formatVersion(version, suffix) {
  return suffix ? `${version}-${suffix}` : version;
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/gu, '\\$&');
}
