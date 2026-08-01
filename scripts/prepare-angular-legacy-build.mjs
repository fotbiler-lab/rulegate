import { cp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, '..');
const sourceDirectory = join(repositoryRoot, 'src', 'Fotbiler.RuleGate.Angular.Legacy');
const builderDirectory = join(repositoryRoot, 'compatibility', 'angular-legacy-builder');
const workDirectory = join(builderDirectory, '.work');

await rm(workDirectory, { force: true, recursive: true });
await mkdir(workDirectory, { recursive: true });

const toolchainManifest = {
  name: '@fotbiler/rulegate-angular-legacy-builder',
  version: '0.0.0',
  private: true,
  devDependencies: {
    '@angular/common': '12.2.17',
    '@angular/compiler': '12.2.17',
    '@angular/compiler-cli': '12.2.17',
    '@angular/core': '12.2.17',
    '@angular/router': '12.2.17',
    '@fotbiler/rulegate-client': 'file:../../src/Fotbiler.RuleGate.Client',
    'ng-packagr': '12.2.7',
    rxjs: '6.6.7',
    tslib: '2.8.1',
    typescript: '4.3.5',
  },
};

await writeFile(
  join(builderDirectory, 'package.json'),
  `${JSON.stringify(toolchainManifest, null, 2)}\n`,
);

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
