export class TextSizeControl {
  constructor(button) {
    this.button = button;
  }

  initialize() {
    this.button?.addEventListener('click', () => {
      const enlarged = document.body.classList.toggle('large-text');
      this.button.setAttribute('aria-pressed', String(enlarged));
      this.button.querySelector('span').textContent = enlarged ? 'Обычный шрифт' : 'Крупный шрифт';
    });
  }
}
