export class SearchDialog {
  constructor(dialog, items) {
    this.dialog = dialog;
    this.items = items;
    this.input = dialog?.querySelector('input');
    this.results = dialog?.querySelector('#search-results');
  }

  initialize() {
    if (!this.dialog) return;
    document.querySelector('#search-open')?.addEventListener('click', () => this.open());
    document.querySelector('#search-close')?.addEventListener('click', () => this.dialog.close());
    this.input.addEventListener('input', () => this.render());
    this.dialog.addEventListener('click', (event) => {
      const bounds = this.dialog.getBoundingClientRect();
      if (
        event.target === this.dialog &&
        (event.clientX < bounds.left ||
          event.clientX > bounds.right ||
          event.clientY < bounds.top ||
          event.clientY > bounds.bottom)
      )
        this.dialog.close();
    });
    if (location.hash === '#search') this.open();
  }

  open() {
    this.dialog.showModal();
    this.render();
    this.input.focus();
  }

  render() {
    const query = this.input.value.trim().toLocaleLowerCase('ru');
    const matches = this.items.filter((item) => item.title.toLocaleLowerCase('ru').includes(query));
    const links = matches.map((item) => {
      const link = document.createElement('a');
      link.href = item.url;
      link.textContent = `${item.title} →`;
      link.addEventListener('click', () => this.dialog.close());
      return link;
    });
    this.results.replaceChildren(...links);
    if (!matches.length) {
      const message = document.createElement('p');
      message.textContent = 'Такого раздела не найдено. Попробуйте другой запрос.';
      this.results.append(message);
    }
  }
}
