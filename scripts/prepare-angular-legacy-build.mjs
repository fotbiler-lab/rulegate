import { cp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, '..');
const sourceDirectory = join(repositoryRoot, 'src', 'Fotbiler.RuleGate.Angular.Legacy');
const workDirectory = join(repositoryRoot, 'compatibility', 'angular-legacy-builder', '.work');

await rm(workDirectory, { force: true, recursive: true });
await mkdir(workDirectory, { recursive: true });

for (const entry of ['README.md', 'ng-package.json', 'package.json', 'src', 'tsconfig.lib.json']) {
  await cp(join(sourceDirectory, entry), join(workDirectory, entry), {
    force: true,
    recursive: true,
  });
}

const manifestPath = join(workDirectory, 'ng-package.json');
const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
manifest.$schema = '../node_modules/ng-packagr/ng-package.schema.json';
manifest.dest = '../../../dist/rulegate-angular-legacy';
await writeFile(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`);
