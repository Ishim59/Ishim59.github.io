export class AudienceTabs {
  constructor(panel, audiences) {
    this.panel = panel;
    this.audiences = audiences;
    this.buttons = [...document.querySelectorAll('[data-audience]')];
  }

  initialize() {
    if (!this.panel) return;
    this.select('parents');
    this.buttons.forEach((button, index) => {
      button.addEventListener('click', () => this.select(button.dataset.audience));
      button.addEventListener('keydown', (event) => {
        const positions = {
          ArrowRight: (index + 1) % this.buttons.length,
          ArrowLeft: (index + this.buttons.length - 1) % this.buttons.length,
          Home: 0,
          End: this.buttons.length - 1,
        };
        if (!(event.key in positions)) return;
        event.preventDefault();
        const next = this.buttons[positions[event.key]];
        next.focus();
        this.select(next.dataset.audience);
      });
    });
  }

  select(audience) {
    this.panel.replaceChildren(
      ...this.audiences[audience].map(([title, description, url]) => {
        const link = document.createElement('a');
        link.href = url;
        const heading = document.createElement('strong');
        heading.textContent = title;
        const paragraph = document.createElement('p');
        paragraph.textContent = description;
        const arrow = document.createElement('b');
        arrow.textContent = '→';
        link.append(heading, paragraph, arrow);
        return link;
      }),
    );
    this.panel.setAttribute('aria-labelledby', `tab-${audience}`);
    this.buttons.forEach((button) => {
      const selected = button.dataset.audience === audience;
      button.setAttribute('aria-selected', String(selected));
      button.tabIndex = selected ? 0 : -1;
    });
  }
}
