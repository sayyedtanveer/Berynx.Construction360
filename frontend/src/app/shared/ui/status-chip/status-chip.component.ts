import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

type StatusType =
  | 'draft'
  | 'pending'
  | 'approved'
  | 'active'
  | 'completed'
  | 'rejected'
  | 'cancelled'
  | 'overdue'
  | 'info'
  | 'success'
  | 'warning'
  | 'danger';

@Component({
  selector: 'app-status-chip',
  imports: [CommonModule],
  template: `
    <span [class]="chipClasses" role="status">
      @if (showDot) {
        <span class="status-dot" aria-hidden="true"></span>
      }
      <ng-content />
    </span>
  `,
  styles: `
    span {
      display: inline-flex;
      align-items: center;
      gap: var(--space-1);
      padding: var(--space-1) var(--space-2);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-medium);
      line-height: 1.25;
      border-radius: var(--radius-full);
      white-space: nowrap;
      text-transform: uppercase;
      letter-spacing: 0.025em;
    }

    .status-dot {
      display: inline-block;
      width: 5px;
      height: 5px;
      border-radius: 50%;
      background: currentColor;
    }

    /* Status-specific styles */
    .status-draft {
      background: #F0EDE8;
      color: var(--status-draft);
    }

    .status-pending {
      background: var(--warning-light);
      color: var(--status-pending);
    }

    .status-approved {
      background: var(--info-light);
      color: var(--status-approved);
    }

    .status-active {
      background: var(--success-light);
      color: var(--status-active);
    }

    .status-completed {
      background: var(--success-light);
      color: var(--status-completed);
    }

    .status-rejected {
      background: var(--danger-light);
      color: var(--status-rejected);
    }

    .status-cancelled {
      background: #F0EDE8;
      color: var(--status-cancelled);
      text-decoration: line-through;
    }

    .status-overdue {
      background: var(--danger-light);
      color: var(--status-overdue);
    }

    .status-info {
      background: var(--info-light);
      color: var(--info);
    }

    .status-success {
      background: var(--success-light);
      color: var(--success);
    }

    .status-warning {
      background: var(--warning-light);
      color: var(--warning);
    }

    .status-danger {
      background: var(--danger-light);
      color: var(--danger);
    }
  `,
})
export class StatusChipComponent {
  @Input() status: StatusType = 'draft';
  @Input() showDot = true;

  get chipClasses(): string {
    return `status-${this.status}`;
  }
}
