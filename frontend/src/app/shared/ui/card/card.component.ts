import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  imports: [CommonModule],
  template: `
    <div [class]="cardClasses">
      @if (title || headerActions) {
        <div class="card-header">
          @if (title) {
            <h3 class="card-title">{{ title }}</h3>
          }
          @if (headerActions) {
            <div class="card-actions">
              <ng-content select="[header-actions]" />
            </div>
          }
        </div>
      }
      <div class="card-content">
        <ng-content />
      </div>
      @if (hasFooter) {
        <div class="card-footer">
          <ng-content select="[footer]" />
        </div>
      }
    </div>
  `,
  styles: `
    .card {
      background: var(--surface);
      border: 1px solid var(--border-default);
      border-radius: var(--radius-lg);
      overflow: hidden;
      position: relative;
    }

    .card::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 2px;
      background: var(--primary-copper);
      opacity: 0;
      transition: opacity var(--transition-fast);
    }

    .card-elevated {
      box-shadow: var(--shadow-md);
    }

    .card-elevated:hover::before {
      opacity: 1;
    }

    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      padding: var(--space-4) var(--space-5);
      border-bottom: 1px solid var(--border-light);
    }

    .card-title {
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
      color: var(--text-heading);
      margin: 0;
    }

    .card-actions {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }

    .card-content {
      padding: var(--space-5);
    }

    .card-compact .card-header,
    .card-compact .card-content,
    .card-compact .card-footer {
      padding: var(--space-3);
    }

    .card-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      padding: var(--space-4) var(--space-5);
      border-top: 1px solid var(--border-light);
      background: var(--canvas);
    }

    .card-hoverable {
      transition: all var(--transition-fast);
      cursor: pointer;
    }

    .card-hoverable:hover {
      border-color: var(--border-medium);
      box-shadow: var(--shadow-lg);
    }
  `,
})
export class CardComponent {
  @Input() title?: string;
  @Input() elevated = false;
  @Input() compact = false;
  @Input() hoverable = false;
  @Input() headerActions = false;
  @Input() hasFooter = false;

  get cardClasses(): string {
    const classes = ['card'];
    if (this.elevated) classes.push('card-elevated');
    if (this.compact) classes.push('card-compact');
    if (this.hoverable) classes.push('card-hoverable');
    return classes.join(' ');
  }
}
