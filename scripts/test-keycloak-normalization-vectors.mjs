#!/usr/bin/env node

import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, '..');
const vectors = JSON.parse(
  await readFile(resolve(repositoryRoot, 'tests/KeycloakRoleNormalizationVectors.json'), 'utf8'),
);
await import('@angular/compiler');
const integration = await import(
  pathToFileURL(
    resolve(
      repositoryRoot,
      'dist/rulegate-angular/fesm2022/fotbiler-rulegate-angular-keycloak.mjs',
    ),
  ).href
);

for (const vector of vectors.components) {
  assert.equal(integration.encodeRuleGateKeycloakComponent(vector.value), vector.encoded);
}

for (const vector of vectors.roles) {
  const normalized =
    vector.scope === 'realm'
      ? integration.ruleGateKeycloakRealmRole(vector.role)
      : integration.ruleGateKeycloakClientRole(vector.clientId, vector.role);

  assert.equal(normalized, vector.normalized);
}

console.log('Angular and .NET Keycloak role normalization vectors are aligned.');
