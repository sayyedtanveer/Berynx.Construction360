import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `
    <section class="empty-state" aria-live="polite">
      <span class="empty-state__icon" aria-hidden="true">—</span>
      <h2>{{ title() }}</h2>
      <p>{{ message() }}</p>
    </section>
  `,
  styles: `
    .empty-state {
      align-items: center;
      background: var(--surface);
      border: 1px dashed var(--border);
      border-radius: 1rem;
      display: flex;
      flex-direction: column;
      padding: clamp(1.5rem, 4vw, 2.5rem);
      text-align: center;
    }

    .empty-state__icon {
      align-items: center;
      background: var(--brand-soft);
      border-radius: 50%;
      color: var(--brand);
      display: inline-flex;
      font-size: 1.25rem;
      height: 2.75rem;
      justify-content: center;
      width: 2.75rem;
    }

    h2 {
      font-size: 1.1rem;
      margin: 1rem 0 0.4rem;
    }

    p {
      color: var(--muted);
      line-height: 1.6;
      margin: 0;
      max-width: 36rem;
    }
  `,
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
}
