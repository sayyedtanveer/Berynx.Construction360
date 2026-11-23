import { Component } from '@angular/core';
import { EmptyStateComponent } from '../../shared/ui/empty-state/empty-state.component';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [EmptyStateComponent],
  template: `
    <main class="page">
      <section class="intro" aria-labelledby="welcome-title">
        <p class="eyebrow">Construction operations, connected</p>
        <h1 id="welcome-title">A clear foundation for work that moves.</h1>
        <p class="intro__summary">
          Berynx Construction 360 is ready for its next implementation gate.
          Your business workflows will be added only after their requirements
          and acceptance criteria are approved.
        </p>
        <div class="status-pill">
          <span class="status-pill__dot" aria-hidden="true"></span>
          Gate 0 foundation
        </div>
      </section>

      <section class="workspace" aria-labelledby="workspace-title">
        <div class="workspace__heading">
          <div>
            <p class="eyebrow">Workspace</p>
            <h2 id="workspace-title">Nothing to review yet</h2>
          </div>
          <span class="workspace__caption">Application shell</span>
        </div>
        <app-empty-state
          title="No business modules are configured"
          message="This starter includes shared application infrastructure only. No tenant, customer, purchasing, delivery, finance, inventory, or project workflows have been created."
        />
      </section>

      <footer class="page-footer">
        <span>Designed for the people building India’s infrastructure.</span>
        <span>Versioned API · Secure-by-default foundation</span>
      </footer>
    </main>
  `,
  styles: `
    .page {
      margin: 0 auto;
      max-width: 76rem;
      padding: clamp(2rem, 6vw, 5rem) clamp(1rem, 4vw, 3rem) 2rem;
    }

    .intro {
      background:
        radial-gradient(circle at 86% 14%, rgb(196 125 45 / 15%), transparent 28%),
        linear-gradient(135deg, #123d38, #1c5c50);
      border-radius: 1.5rem;
      color: white;
      overflow: hidden;
      padding: clamp(2rem, 6vw, 4.5rem);
      position: relative;
    }

    .eyebrow {
      color: var(--accent);
      font-size: 0.75rem;
      font-weight: 750;
      letter-spacing: 0.11em;
      margin: 0 0 0.75rem;
      text-transform: uppercase;
    }

    .intro .eyebrow {
      color: #e8b878;
    }

    h1 {
      font-size: clamp(2.25rem, 5vw, 4.25rem);
      letter-spacing: -0.045em;
      line-height: 1.06;
      margin: 0;
      max-width: 48rem;
    }

    .intro__summary {
      color: rgb(255 255 255 / 82%);
      font-size: clamp(1rem, 1.5vw, 1.125rem);
      line-height: 1.7;
      margin: 1.25rem 0 1.75rem;
      max-width: 44rem;
    }

    .status-pill {
      align-items: center;
      background: rgb(255 255 255 / 12%);
      border: 1px solid rgb(255 255 255 / 20%);
      border-radius: 999px;
      display: inline-flex;
      font-size: 0.875rem;
      font-weight: 650;
      gap: 0.55rem;
      padding: 0.5rem 0.85rem;
    }

    .status-pill__dot {
      background: #91d4a2;
      border-radius: 50%;
      height: 0.5rem;
      width: 0.5rem;
    }

    .workspace {
      margin-top: clamp(2.5rem, 6vw, 4rem);
    }

    .workspace__heading {
      align-items: end;
      display: flex;
      justify-content: space-between;
      margin-bottom: 1.25rem;
    }

    .workspace__heading h2 {
      font-size: clamp(1.4rem, 2.5vw, 1.9rem);
      letter-spacing: -0.025em;
      margin: 0;
    }

    .workspace__caption {
      color: var(--muted);
      font-size: 0.875rem;
    }

    .page-footer {
      border-top: 1px solid var(--border);
      color: var(--muted);
      display: flex;
      flex-wrap: wrap;
      font-size: 0.8rem;
      gap: 0.5rem 1.5rem;
      justify-content: space-between;
      margin-top: clamp(3rem, 7vw, 5rem);
      padding-top: 1rem;
    }

    @media (max-width: 40rem) {
      .workspace__heading {
        align-items: start;
        flex-direction: column;
        gap: 0.6rem;
      }

      .page-footer {
        flex-direction: column;
      }
    }
  `,
})
export class HomePage {}
