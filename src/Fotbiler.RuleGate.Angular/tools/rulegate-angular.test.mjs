import assert from 'node:assert/strict';
import { mkdtemp, readFile, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import {
  generateRuleGateTypeScript,
  ruleGateAngularExitCodes,
  runRuleGateAngularCli,
} from './rulegate-angular.mjs';

const manifest = `schemaVersion: 1
application:
  id: sample
  name: Sample
policies:
  - id: documents-read
    resourceType: document
    action: read
    requirement:
      any:
        - permission: documents.read
        - role: documents.reader
  - id: documents-write
    resourceType: document
    action: write
    requirement:
      all:
        - permission: documents.write
        - not:
            role: documents.blocked
`;

test('generates deterministic identifiers and literal types', () => {
  const result = generateRuleGateTypeScript(manifest);

  assert.equal(result.success, true);
  assert.match(result.source, /documentsRead: "documents\.read"/);
  assert.match(result.source, /documentsWrite: "documents-write"/);
  assert.match(result.source, /export type RuleGatePermission/);
  assert.match(result.source, /documentsReader: "documents\.reader"/);
  assert.match(result.source, /export type RuleGateRole/);
  assert.deepEqual(result.counts, {
    policies: 2,
    permissions: 2,
    roles: 2,
    resourceTypes: 1,
    actions: 2,
  });

  const reordered = generateRuleGateTypeScript(`schemaVersion: 1
application:
  name: Sample
  id: sample
policies:
  - id: documents-write
    resourceType: document
    action: write
    requirement:
      all:
        - not:
            role: documents.blocked
        - permission: documents.write
  - id: documents-read
    resourceType: document
    action: read
    requirement:
      any:
        - role: documents.reader
        - permission: documents.read
`);

  assert.equal(reordered.success, true);
  assert.equal(reordered.source, result.source);
});

test('fails closed for duplicate generated identifiers', () => {
  const result = generateRuleGateTypeScript(`schemaVersion: 1
policies:
  - id: documents-read
    resourceType: document
    action: read
    requirement:
      permission: documents.read
  - id: documents_read
    resourceType: document
    action: list
    requirement:
      permission: documents.list
`);

  assert.equal(result.success, false);
  assert.equal(result.source, null);
  assert.match(result.diagnostics.join('\n'), /all map to 'documentsRead'/);
});

test('fails closed for malformed requirement trees and YAML', () => {
  const invalidRequirement = generateRuleGateTypeScript(`schemaVersion: 1
policies:
  - id: documents-read
    resourceType: document
    action: read
    requirement:
      permission: documents.read
      role: documents.reader
`);

  assert.equal(invalidRequirement.success, false);
  assert.match(invalidRequirement.diagnostics.join('\n'), /exactly one requirement kind/);

  const invalidYaml = generateRuleGateTypeScript('schemaVersion: [');
  assert.equal(invalidYaml.success, false);
  assert.match(invalidYaml.diagnostics.join('\n'), /YAML:/);
});

test('accepts backend requirement kinds while collecting frontend identifiers', () => {
  const result = generateRuleGateTypeScript(`schemaVersion: 1
policies:
  - id: secure-access
    resourceType: portal
    action: access
    requirement:
      all:
        - permission: portal.access
        - attributeComparison:
            left: { source: resource, name: ownerId }
            operator: equal
            right: { source: subject, name: id }
        - timeWindow:
            days: [monday]
            start: "08:00"
            end: "18:00"
            timeZone: Europe/Istanbul
        - dateTimeWindow:
            endsAt: "2026-08-01T00:00:00Z"
        - contextAge:
            timestamp: mfa
            maximumAge: "00:15:00"
        - context:
            property: trustedDevice
            operator: equal
            valueType: boolean
            value: true
`);

  assert.equal(result.success, true);
  assert.match(result.source, /portalAccess: "portal\.access"/);
  assert.deepEqual(result.counts, {
    policies: 1,
    permissions: 1,
    roles: 0,
    resourceTypes: 1,
    actions: 1,
  });
});

test('writes atomically and checks byte-exact generated output', async () => {
  const directory = await mkdtemp(join(tmpdir(), 'rulegate-angular-generator-'));
  const manifestPath = join(directory, 'rulegate.yaml');
  const outputPath = join(directory, 'generated', 'rulegate.ts');
  await writeFile(manifestPath, manifest, 'utf8');

  const generated = await runRuleGateAngularCli(
    ['generate', manifestPath, '--output', outputPath],
    createIo(),
  );
  assert.equal(generated, ruleGateAngularExitCodes.success);
  assert.match(await readFile(outputPath, 'utf8'), /RuleGateIdentifiers/);

  const current = await runRuleGateAngularCli(
    ['generate', manifestPath, '--output', outputPath, '--check'],
    createIo(),
  );
  assert.equal(current, ruleGateAngularExitCodes.success);

  await writeFile(outputPath, '// stale\n', 'utf8');
  const stale = await runRuleGateAngularCli(
    ['generate', manifestPath, '--output', outputPath, '--check'],
    createIo(),
  );
  assert.equal(stale, ruleGateAngularExitCodes.invalid);
  assert.equal(await readFile(outputPath, 'utf8'), '// stale\n');

  await writeFile(manifestPath, 'schemaVersion: [', 'utf8');
  const invalid = await runRuleGateAngularCli(
    ['generate', manifestPath, '--output', outputPath],
    createIo(),
  );
  assert.equal(invalid, ruleGateAngularExitCodes.invalid);
  assert.equal(await readFile(outputPath, 'utf8'), '// stale\n');
});

test('returns usage errors without reading files', async () => {
  const io = createIo();
  const result = await runRuleGateAngularCli(['generate', '--check'], io);

  assert.equal(result, ruleGateAngularExitCodes.usage);
  assert.match(io.errorText(), /--check requires --output/);
});

function createIo() {
  let output = '';
  let error = '';

  return {
    stdout: { write: (value) => (output += value) },
    stderr: { write: (value) => (error += value) },
    outputText: () => output,
    errorText: () => error,
  };
}
