import { cp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, '..');
const sourceDirectory = join(repositoryRoot, 'src', 'Fotbiler.RuleGate.Angular');
const workDirectory = join(repositoryRoot, 'compatibility', 'angular-modern-builder', '.work');

await rm(workDirectory, { force: true, recursive: true });
await mkdir(workDirectory, { recursive: true });

for (const entry of [
  'README.md',
  'keycloak',
  'ng-package.json',
  'package.json',
  'src',
  'tools',
  'tsconfig.lib.json',
  'tsconfig.lib.prod.json',
]) {
  await cp(join(sourceDirectory, entry), join(workDirectory, entry), {
    force: true,
    recursive: true,
  });
}

await updateJson(join(workDirectory, 'ng-package.json'), (manifest) => {
  manifest.$schema = '../node_modules/ng-packagr/ng-package.schema.json';
  manifest.dest = '../../../dist/rulegate-angular';
});

for (const name of ['tsconfig.lib.json', 'tsconfig.lib.prod.json']) {
  await updateJson(join(workDirectory, name), (configuration) => {
    configuration.extends = '../../../tsconfig.json';
  });
}

await updateJson(join(workDirectory, 'keycloak', 'ng-package.json'), (manifest) => {
  manifest.$schema = '../../node_modules/ng-packagr/ng-package.schema.json';
});

async function updateJson(path, update) {
  const value = JSON.parse(await readFile(path, 'utf8'));
  update(value);
  await writeFile(path, `${JSON.stringify(value, null, 2)}\n`);
}
