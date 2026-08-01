#!/usr/bin/env node

import { access, readdir, readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, '..');
const ignoredDirectories = new Set([
  '.git',
  '.angular',
  'artifacts',
  'bin',
  'dist',
  'node_modules',
  'obj',
  'out-tsc',
]);

const guideFiles = [
  'README.md',
  '01-Authorization-Foundations.md',
  '02-Packages-and-Installation.md',
  '03-First-Protected-API.md',
  '04-Policy-Language.md',
  '05-ASP.NET-Core-Integration.md',
  '06-Trusted-Attributes-and-Context.md',
  '07-Identity-and-Keycloak.md',
  '08-Frontend-Integration.md',
  '09-CLI-and-Policy-Lifecycle.md',
  '10-Testing-and-Diagnostics.md',
  '11-Policy-Sources-and-Reload.md',
  '12-Extensibility.md',
  '13-Real-World-Recipes.md',
  '14-Production-Checklist.md',
  'Glossary.md',
];

const markdownFiles = [];
await collectMarkdown(repositoryRoot);

const errors = [];
let checkedLinks = 0;

for (const guideFile of guideFiles) {
  const fullPath = path.join(repositoryRoot, 'docs', 'guide', guideFile);
  await assertExists(fullPath, `Missing guide chapter: docs/guide/${guideFile}`);
}

for (const markdownFile of markdownFiles.sort()) {
  const content = await readFile(markdownFile, 'utf8');
  const links = extractLinksOutsideCodeFences(content);

  for (const rawTarget of links) {
    const target = normalizeTarget(rawTarget);

    if (!target || shouldSkip(target)) {
      continue;
    }

    checkedLinks += 1;

    const [pathPart, fragment = ''] = target.split('#', 2);
    const decodedPath = decodeURIComponent(pathPart);
    const resolvedPath = path.resolve(path.dirname(markdownFile), decodedPath || '.');

    if (!isInsideRepository(resolvedPath)) {
      errors.push(formatError(markdownFile, target, 'target escapes the repository'));
      continue;
    }

    try {
      const targetStat = await stat(resolvedPath);

      if (fragment && targetStat.isFile() && resolvedPath.endsWith('.md')) {
        const targetContent = await readFile(resolvedPath, 'utf8');
        const anchors = collectHeadingAnchors(targetContent);
        const decodedFragment = decodeURIComponent(fragment).toLowerCase();

        if (!anchors.has(decodedFragment)) {
          errors.push(formatError(markdownFile, target, `heading '#${fragment}' was not found`));
        }
      }
    } catch {
      errors.push(formatError(markdownFile, target, 'target does not exist'));
    }
  }
}

if (errors.length > 0) {
  console.error(`Documentation verification failed with ${errors.length} error(s):`);

  for (const error of errors) {
    console.error(`- ${error}`);
  }

  process.exit(1);
}

console.log(
  `Documentation verification passed: ${markdownFiles.length} Markdown files, ${checkedLinks} local links, ${guideFiles.length} guide pages.`,
);

async function collectMarkdown(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      if (!ignoredDirectories.has(entry.name)) {
        await collectMarkdown(path.join(directory, entry.name));
      }

      continue;
    }

    if (entry.isFile() && entry.name.endsWith('.md')) {
      markdownFiles.push(path.join(directory, entry.name));
    }
  }
}

async function assertExists(file, message) {
  try {
    await access(file);
  } catch {
    errors.push(message);
  }
}

function extractLinksOutsideCodeFences(markdown) {
  const links = [];
  let inFence = false;

  for (const line of markdown.split(/\r?\n/u)) {
    if (/^\s*(```|~~~)/u.test(line)) {
      inFence = !inFence;
      continue;
    }

    if (inFence) {
      continue;
    }

    const markdownLink = /!?\[[^\]]*\]\(([^)]+)\)/gu;
    const htmlLink = /\b(?:href|src)=["']([^"']+)["']/gu;

    for (const match of line.matchAll(markdownLink)) {
      links.push(match[1]);
    }

    for (const match of line.matchAll(htmlLink)) {
      links.push(match[1]);
    }
  }

  return links;
}

function normalizeTarget(rawTarget) {
  let target = rawTarget.trim();

  if (target.startsWith('<') && target.includes('>')) {
    target = target.slice(1, target.indexOf('>'));
  } else {
    target = target.replace(/\s+["'][^"']*["']$/u, '');
  }

  return target;
}

function shouldSkip(target) {
  return target.startsWith('#') || target.startsWith('/') || /^[a-z][a-z0-9+.-]*:/iu.test(target);
}

function isInsideRepository(target) {
  const relative = path.relative(repositoryRoot, target);
  return relative === '' || (!relative.startsWith('..') && !path.isAbsolute(relative));
}

function collectHeadingAnchors(markdown) {
  const anchors = new Set();
  const occurrences = new Map();
  let inFence = false;

  for (const line of markdown.split(/\r?\n/u)) {
    if (/^\s*(```|~~~)/u.test(line)) {
      inFence = !inFence;
      continue;
    }

    if (inFence) {
      continue;
    }

    const match = /^(?: {0,3})#{1,6}\s+(.+?)\s*#*$/u.exec(line);
    if (!match) {
      continue;
    }

    const base = match[1]
      .replace(/[`*_~]/gu, '')
      .toLowerCase()
      .replace(/[^\p{Letter}\p{Number}\s-]/gu, '')
      .trim()
      .replace(/\s+/gu, '-');
    const occurrence = occurrences.get(base) ?? 0;
    occurrences.set(base, occurrence + 1);
    anchors.add(occurrence === 0 ? base : `${base}-${occurrence}`);
  }

  return anchors;
}

function formatError(source, target, message) {
  return `${path.relative(repositoryRoot, source)} -> ${target}: ${message}`;
}
