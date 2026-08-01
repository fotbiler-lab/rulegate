import { mkdir, readFile, readdir, writeFile } from 'node:fs/promises';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, '..');

const baselineVersion = '0.9.0-preview.4';
const update = process.argv.includes('--update');

const baselineDirectory = join(repositoryRoot, 'api-baselines', 'frontend', baselineVersion);

function normalizeDeclaration(content) {
  let normalized = content.replace(/\r\n/g, '\n');

  // Documentation changes do not change the TypeScript API contract.
  normalized = normalized.replace(/\/\*[\s\S]*?\*\//g, '');

  const lines = normalized
    .split('\n')
    .map((line) => line.replace(/[ \t]+$/g, ''))
    .filter((line) => {
      const trimmed = line.trim();

      // Generated declaration-map metadata is not API.
      if (trimmed.startsWith('//# sourceMappingURL=')) {
        return false;
      }

      if (trimmed.startsWith('///')) {
        return false;
      }

      // Private implementation details are not public contract.
      if (/^private(?:\s|$)/.test(trimmed)) {
        return false;
      }

      // Angular compiler metadata is generated implementation detail.
      if (/^static ɵ(?:fac|prov|dir|cmp|mod|inj|pipe):/.test(trimmed)) {
        return false;
      }

      return true;
    })
    .join('\n')
    .replace(/\n{3,}/g, '\n\n')
    .trim();

  return `${lines.length === 0 ? '' : lines}\n`;
}

async function collectDeclarations(directory) {
  const results = [];

  async function visit(current) {
    const entries = await readdir(current, { withFileTypes: true });

    entries.sort((left, right) => left.name.localeCompare(right.name));

    for (const entry of entries) {
      const absolute = join(current, entry.name);

      if (entry.isDirectory()) {
        await visit(absolute);
        continue;
      }

      if (entry.isFile() && entry.name.endsWith('.d.ts')) {
        results.push(absolute);
      }
    }
  }

  await visit(directory);
  return results;
}

async function renderSnapshot(files, baseDirectory) {
  const sections = [];

  for (const file of files) {
    const relativePath = relative(baseDirectory, file).replaceAll('\\', '/');
    const content = await readFile(file, 'utf8');

    sections.push(`===== ${relativePath} =====\n${normalizeDeclaration(content)}`);
  }

  return `${sections.join('\n').trim()}\n`;
}

const clientDirectory = join(repositoryRoot, 'src', 'Fotbiler.RuleGate.Client', 'dist');

const modernDirectory = join(repositoryRoot, 'dist', 'rulegate-angular');

const legacyDirectory = join(repositoryRoot, 'dist', 'rulegate-angular-legacy');

const contracts = [
  {
    name: 'client',
    baseline: 'client.api.txt',
    baseDirectory: clientDirectory,
    files: await collectDeclarations(clientDirectory),
  },
  {
    name: 'angular',
    baseline: 'angular.api.txt',
    baseDirectory: modernDirectory,
    files: [join(modernDirectory, 'index.d.ts')],
  },
  {
    name: 'angular-keycloak',
    baseline: 'angular-keycloak.api.txt',
    baseDirectory: modernDirectory,
    files: [join(modernDirectory, 'keycloak', 'index.d.ts')],
  },
  {
    name: 'angular-legacy',
    baseline: 'angular-legacy.api.txt',
    baseDirectory: legacyDirectory,
    files: await collectDeclarations(legacyDirectory),
  },
];

await mkdir(baselineDirectory, { recursive: true });

let failed = false;

for (const contract of contracts) {
  const actual = await renderSnapshot(contract.files, contract.baseDirectory);

  const baselinePath = join(baselineDirectory, contract.baseline);

  if (update) {
    await writeFile(baselinePath, actual, 'utf8');
    console.log(`Updated frontend API baseline: ${contract.name}`);
    continue;
  }

  let expected;

  try {
    expected = await readFile(baselinePath, 'utf8');
  } catch {
    console.error(`ERROR: Frontend API baseline is missing: ${contract.baseline}`);
    failed = true;
    continue;
  }

  if (actual !== expected) {
    console.error(`ERROR: Frontend public API changed: ${contract.name}`);
    console.error(`Baseline: api-baselines/frontend/${baselineVersion}/${contract.baseline}`);
    failed = true;
    continue;
  }

  console.log(`Frontend API unchanged: ${contract.name}`);
}

if (failed) {
  console.error(
    'Frontend API freeze verification failed. Review the public contract before updating a baseline.',
  );
  process.exit(1);
}

if (!update) {
  console.log(`Frontend API freeze verified against ${baselineVersion}.`);
}
