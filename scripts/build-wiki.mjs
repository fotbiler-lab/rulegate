#!/usr/bin/env node

import { mkdir, readFile, rm, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, '..');
const guideDirectory = path.join(repositoryRoot, 'docs', 'guide');
const outputDirectory = path.join(repositoryRoot, 'artifacts', 'wiki');
const repositoryUrl = 'https://github.com/fotbiler-lab/rulegate';

const pages = [
  ['README.md', 'Home.md', 'Home'],
  [
    '01-Authorization-Foundations.md',
    '01-Authorization-Foundations.md',
    '1. Authorization foundations',
  ],
  [
    '02-Packages-and-Installation.md',
    '02-Packages-and-Installation.md',
    '2. Packages and installation',
  ],
  ['03-First-Protected-API.md', '03-First-Protected-API.md', '3. First protected API'],
  ['04-Policy-Language.md', '04-Policy-Language.md', '4. Policy language'],
  [
    '05-ASP.NET-Core-Integration.md',
    '05-ASP.NET-Core-Integration.md',
    '5. ASP.NET Core integration',
  ],
  [
    '06-Trusted-Attributes-and-Context.md',
    '06-Trusted-Attributes-and-Context.md',
    '6. Trusted attributes and context',
  ],
  ['07-Identity-and-Keycloak.md', '07-Identity-and-Keycloak.md', '7. Identity and Keycloak'],
  ['08-Frontend-Integration.md', '08-Frontend-Integration.md', '8. Frontend integration'],
  [
    '09-CLI-and-Policy-Lifecycle.md',
    '09-CLI-and-Policy-Lifecycle.md',
    '9. CLI and policy lifecycle',
  ],
  ['10-Testing-and-Diagnostics.md', '10-Testing-and-Diagnostics.md', '10. Testing and diagnostics'],
  [
    '11-Policy-Sources-and-Reload.md',
    '11-Policy-Sources-and-Reload.md',
    '11. Policy sources and reload',
  ],
  ['12-Extensibility.md', '12-Extensibility.md', '12. Extensibility'],
  ['13-Real-World-Recipes.md', '13-Real-World-Recipes.md', '13. Real-world recipes'],
  ['14-Production-Checklist.md', '14-Production-Checklist.md', '14. Production checklist'],
  ['Glossary.md', 'Glossary.md', 'Glossary'],
];

await rm(outputDirectory, { recursive: true, force: true });
await mkdir(outputDirectory, { recursive: true });

for (const [sourceName, outputName] of pages) {
  const sourcePath = path.join(guideDirectory, sourceName);
  const outputPath = path.join(outputDirectory, outputName);
  const markdown = await readFile(sourcePath, 'utf8');
  const transformed = await rewriteLinks(markdown, sourcePath);
  await writeFile(outputPath, transformed, 'utf8');
}

const sidebar = [
  '# RuleGate Guide',
  '',
  ...pages.map(([, outputName, title]) => `- [${title}](${outputName.replace(/\.md$/u, '')})`),
  '',
  '## Project',
  '',
  `- [Repository](${repositoryUrl})`,
  `- [Reference documentation](${repositoryUrl}/tree/main/docs)`,
  `- [Latest release](${repositoryUrl}/releases/latest)`,
  `- [Security](${repositoryUrl}/blob/main/docs/security.md)`,
  `- [Roadmap](${repositoryUrl}/blob/main/docs/roadmap.md)`,
  '',
].join('\n');

const footer = [
  '---',
  '',
  `Canonical source: [\`docs/guide\`](${repositoryUrl}/tree/main/docs/guide) · ` +
    `[Documentation index](${repositoryUrl}/blob/main/docs/README.md) · ` +
    `[RuleGate ${await readStableVersion()}](${repositoryUrl}/releases/latest)`,
  '',
].join('\n');

await writeFile(path.join(outputDirectory, '_Sidebar.md'), sidebar, 'utf8');
await writeFile(path.join(outputDirectory, '_Footer.md'), footer, 'utf8');

console.log(`Wiki build completed: ${pages.length + 2} pages in ${outputDirectory}`);

async function rewriteLinks(markdown, sourcePath) {
  const pattern = /(!?\[[^\]]*\]\()([^)]+)(\))/gu;
  let result = '';
  let cursor = 0;

  for (const match of markdown.matchAll(pattern)) {
    result += markdown.slice(cursor, match.index);
    const rewritten = await rewriteTarget(match[2], sourcePath);
    result += `${match[1]}${rewritten}${match[3]}`;
    cursor = match.index + match[0].length;
  }

  return result + markdown.slice(cursor);
}

async function rewriteTarget(rawTarget, sourcePath) {
  const target = rawTarget.trim();

  if (target.startsWith('#') || target.startsWith('/') || /^[a-z][a-z0-9+.-]*:/iu.test(target)) {
    return target;
  }

  const hashIndex = target.indexOf('#');
  const filePart = hashIndex >= 0 ? target.slice(0, hashIndex) : target;
  const fragment = hashIndex >= 0 ? target.slice(hashIndex) : '';
  const resolved = path.resolve(path.dirname(sourcePath), decodeURIComponent(filePart));

  if (resolved.startsWith(`${guideDirectory}${path.sep}`)) {
    const sourceName = path.basename(resolved);
    const page = pages.find(([candidate]) => candidate === sourceName);

    if (page) {
      return `${page[1].replace(/\.md$/u, '')}${fragment}`;
    }
  }

  const relative = path.relative(repositoryRoot, resolved).split(path.sep).join('/');
  const targetStat = await stat(resolved);
  const route = targetStat.isDirectory() ? 'tree' : 'blob';
  return `${repositoryUrl}/${route}/main/${relative}${fragment}`;
}

async function readStableVersion() {
  const changelog = await readFile(path.join(repositoryRoot, 'CHANGELOG.md'), 'utf8');
  return /^## \[([^\]]+)\] - /mu.exec(changelog)?.[1] ?? 'stable';
}
