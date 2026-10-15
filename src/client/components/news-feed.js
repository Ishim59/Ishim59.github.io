/** Replaces the static news snapshot only when a real API is configured. */
export class NewsFeed {
  constructor(container, apiBase) {
    this.container = container;
    this.apiBase = apiBase;
  }

  async initialize() {
    if (!this.container || !this.apiBase) return;
    try {
      const response = await fetch(`${this.apiBase.replace(/\/$/, '')}/news`);
      if (!response.ok) throw new Error('News API is unavailable.');
      const articles = await response.json();
      const nodes = articles.map((article) => {
        const link = document.createElement('a');
        // The API also serves the full article; a separately hosted API keeps its origin.
        const apiOrigin = new URL(this.apiBase, location.origin).origin;
        link.href = `${apiOrigin}/news/${encodeURIComponent(article.slug)}/`;
        const date = document.createElement('small');
        date.textContent = article.publishedAt;
        const title = document.createElement('h2');
        title.textContent = `${article.title} →`;
        const summary = document.createElement('p');
        summary.textContent = article.summary;
        link.append(date, title, summary);
        return link;
      });
      this.container.replaceChildren(...nodes);
    } catch {
      const status = document.createElement('p');
      status.textContent = 'Не удалось обновить новости. Показана сохранённая версия.';
      status.setAttribute('role', 'status');
      this.container.after(status);
    }
  }
}
