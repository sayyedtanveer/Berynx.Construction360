import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  standalone: true,
  template: `
    <div class="loading-state" role="status" aria-live="polite">
      <span class="loading-state__spinner" aria-hidden="true"></span>
      <span>{{ label() }}</span>
    </div>
  `,
  styles: `
    .loading-state {
      align-items: center;
      color: var(--muted);
      display: flex;
      gap: 0.75rem;
      min-height: 4rem;
    }

    .loading-state__spinner {
      animation: spin 0.8s linear infinite;
      border: 2px solid var(--border);
      border-radius: 50%;
      border-top-color: var(--brand);
      height: 1.25rem;
      width: 1.25rem;
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .loading-state__spinner {
        animation-duration: 2s;
      }
    }
  `,
})
export class LoadingStateComponent {
  readonly label = input('Loading…');
}
