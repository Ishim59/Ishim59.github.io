import { readFile, writeFile, readdir } from 'node:fs/promises';
import { parseFragment, serialize } from 'parse5';

// Imported HTML may contain invalid nested paragraphs and links.
// Normalize it with the same parsing rules browsers use, before formatting it.
for (const file of await readdir('content/pages')) {
  if (!file.endsWith('.html')) continue;
  const path = `content/pages/${file}`;
  await writeFile(path, serialize(parseFragment(await readFile(path, 'utf8'))));
}
console.log('Normalized imported HTML fragments.');
