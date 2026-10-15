import { readFile, writeFile, mkdir, readdir, copyFile, cp } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { Layout, escapeHtml } from '../src/components/layout.js';
import prettier from 'prettier';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2);
const option = (name, fallback) => (args.includes(name) ? args[args.indexOf(name) + 1] : fallback);
const output = path.resolve(root, option('--output', '.'));
if (output !== root && !output.startsWith(root + path.sep))
  throw new Error('Output must stay inside the repository.');
const json = async (file) => JSON.parse(await readFile(path.join(root, file), 'utf8'));
const site = await json('content/site.json');
site.apiBaseUrl = option('--api-base', site.apiBaseUrl);
const pages = await json('content/pages.json');
const news = await json('content/news.json');
const layout = await Layout.load(root, site);

async function save(relative, content) {
  const target = path.join(output, relative);
  await mkdir(path.dirname(target), { recursive: true });
  const parser = { '.html': 'html', '.json': 'json', '.css': 'css' }[path.extname(relative)];
  const options = await prettier.resolveConfig(root);
  await writeFile(
    target,
    parser ? await prettier.format(content, { ...options, parser }) : content,
  );
}

function newsList() {
  return `<div class="article-news" data-news-list>${news
    .map(
      (item) =>
        `<a href="/news/${item.slug}/"><small>${escapeHtml(item.publishedAt)}</small><h2>${escapeHtml(item.title)} →</h2><p>${escapeHtml(item.summary)}</p></a>`,
    )
    .join('\n')}</div>`;
}

for (const page of pages) {
  const body =
    page.slug === 'news'
      ? newsList()
      : await readFile(path.join(root, 'content/pages', page.file), 'utf8');
  await save(`${page.slug}/index.html`, layout.article(page.title, body));
  // Keep published links and browser bookmarks working after renaming routes.
  await save(
    page.legacyUrl.slice(1),
    `<!doctype html><html lang="ru"><head><meta charset="utf-8"><title>${escapeHtml(page.title)}</title><link rel="canonical" href="${page.url}"><meta http-equiv="refresh" content="0; url=${page.url}"></head><body><a href="${page.url}">Перейти в раздел «${escapeHtml(page.title)}»</a></body></html>`,
  );
}
const home = await readFile(path.join(root, 'content/home.html'), 'utf8');
await save('index.html', layout.render(site.name, home, { home: true }));
await save(
  '404.html',
  layout.article('Страница не найдена', '<p>Проверьте адрес или выберите раздел в меню.</p>'),
);
await save('assets/data/site.json', JSON.stringify({ ...site, pages }, null, 2));
await save('assets/data/news.json', JSON.stringify(news, null, 2));
await save('news/article-template.html', layout.article('__NEWS_TITLE__', '__NEWS_CONTENT__'));

const styles = ['base', 'layout', 'home', 'sections', 'responsive', 'articles'];
const css = await Promise.all(
  styles.map((name) => readFile(path.join(root, 'src/styles', `${name}.css`), 'utf8')),
);
await save('assets/css/site.css', '/* Generated from src/styles/. */\n' + css.join('\n'));
await cp(path.join(root, 'src/client'), path.join(output, 'assets/js'), { recursive: true });
if (output !== root) {
  for (const entry of await readdir(path.join(root, 'assets'), { withFileTypes: true })) {
    if (['js', 'css', 'data'].includes(entry.name)) continue;
    await cp(path.join(root, 'assets', entry.name), path.join(output, 'assets', entry.name), {
      recursive: true,
    });
  }
}
console.log(`Built ${pages.length} pages in ${path.relative(root, output) || 'repository root'}.`);
