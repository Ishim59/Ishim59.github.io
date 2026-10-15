export class NavigationMenu {
  constructor(button, navigation) {
    this.button = button;
    this.navigation = navigation;
  }

  initialize() {
    if (!this.button || !this.navigation) return;
    this.button.addEventListener('click', () =>
      this.setOpen(!this.navigation.classList.contains('open')),
    );
    this.navigation
      .querySelectorAll('a')
      .forEach((link) => link.addEventListener('click', () => this.setOpen(false)));
    document.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') this.setOpen(false);
    });
  }

  setOpen(open) {
    this.navigation.classList.toggle('open', open);
    this.button.setAttribute('aria-expanded', String(open));
  }
}
