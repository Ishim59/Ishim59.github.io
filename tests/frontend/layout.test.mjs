import test from 'node:test';
import assert from 'node:assert/strict';
import { Layout, escapeHtml } from '../../src/components/layout.js';

test('Imported markup cannot escape the page title', () => {
  const layout = new Layout({ header: '', footer: '', 'search-dialog': '' }, { navigation: [] });
  const rendered = layout.article(
    '<img src=x onerror=alert(1)>',
    '<p>Trusted repository content</p>',
  );
  assert.ok(!rendered.includes('<h1><img'));
  assert.ok(rendered.includes('&lt;img'));
});

test('Title escaping handles attribute delimiters and ampersands', () => {
  assert.equal(escapeHtml('"<school>&'), '&quot;&lt;school&gt;&amp;');
});
