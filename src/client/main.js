import { NavigationMenu } from './components/navigation-menu.js';
import { SearchDialog } from './components/search-dialog.js';
import { AudienceTabs } from './components/audience-tabs.js';
import { TextSizeControl } from './components/text-size-control.js';
import { NewsFeed } from './components/news-feed.js';

new NavigationMenu(document.querySelector('#menu'), document.querySelector('#nav')).initialize();
new TextSizeControl(document.querySelector('#vision')).initialize();

try {
  const response = await fetch('/assets/data/site.json');
  if (!response.ok) throw new Error('Site configuration is unavailable.');
  const site = await response.json();
  new SearchDialog(document.querySelector('#search-dialog'), [
    ...site.pages,
    ...(site.searchLinks ?? []),
  ]).initialize();
  new AudienceTabs(document.querySelector('#audience-panel'), site.audiences).initialize();
  await new NewsFeed(document.querySelector('[data-news-list]'), site.apiBaseUrl).initialize();
} catch (error) {
  console.error('Could not initialize interactive sections:', error.message);
  const searchButton = document.querySelector('#search-open');
  if (searchButton) {
    searchButton.disabled = true;
    searchButton.title = 'Поиск временно недоступен. Используйте меню разделов.';
  }
}
