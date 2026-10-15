import { readFile, stat } from 'node:fs/promises';
import path from 'node:path';
const root = process.cwd();
const pages = JSON.parse(await readFile('content/pages.json', 'utf8'));
const errors = [];
let references = 0;
for (const page of [{ slug: '' }, ...pages]) {
  const file = path.join(root, page.slug, 'index.html');
  const html = await readFile(file, 'utf8');
  for (const match of html.matchAll(/(?:href|src)="([^"]+)"/g)) {
    const url = match[1];
    if (!url.startsWith('/') || url.startsWith('//')) continue;
    references++;
    const pathname = new URL(url, 'https://site.test').pathname;
    const target = path.join(root, pathname, pathname.endsWith('/') ? 'index.html' : '');
    try {
      if (!(await stat(target)).isFile()) throw new Error();
    } catch {
      errors.push(`${page.slug || '/'}: missing ${pathname}`);
    }
  }
}
if (errors.length) {
  console.error(errors.join('\n'));
  process.exitCode = 1;
} else console.log(`Checked ${pages.length + 1} pages and ${references} local references.`);
