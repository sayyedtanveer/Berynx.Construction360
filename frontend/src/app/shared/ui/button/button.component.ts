import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-button',
  imports: [CommonModule],
  template: `
    <button
      [type]="type"
      [disabled]="disabled || loading"
      [class]="buttonClasses"
      [attr.aria-busy]="loading"
    >
      @if (loading) {
        <span class="btn-spinner" aria-hidden="true"></span>
      }
      <ng-content />
    </button>
  `,
  styles: `
    button {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-4);
      height: var(--height-button);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      white-space: nowrap;
      transition: all var(--transition-fast);
      cursor: pointer;
      border: 1px solid transparent;
    }

    button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    /* Primary Button — Copper Accent */
    .btn-primary {
      background: var(--primary-copper);
      color: var(--text-inverse);
      border-color: var(--primary-copper);
      box-shadow: 0 1px 2px rgba(196, 106, 54, 0.15);
    }

    .btn-primary:hover:not(:disabled) {
      background: var(--primary-copper-hover);
      border-color: var(--primary-copper-hover);
      box-shadow: 0 2px 4px rgba(196, 106, 54, 0.2);
    }

    /* Secondary Button */
    .btn-secondary {
      background: var(--surface);
      color: var(--text-heading);
      border-color: var(--border-default);
    }

    .btn-secondary:hover:not(:disabled) {
      background: var(--canvas);
      border-color: var(--border-medium);
    }

    /* Ghost Button */
    .btn-ghost {
      background: transparent;
      color: var(--text-secondary);
      border-color: transparent;
    }

    .btn-ghost:hover:not(:disabled) {
      background: var(--canvas);
      color: var(--text-heading);
    }

    /* Danger Button */
    .btn-danger {
      background: var(--danger);
      color: var(--text-inverse);
      border-color: var(--danger);
    }

    .btn-danger:hover:not(:disabled) {
      background: #A33628;
      border-color: #A33628;
    }

    /* Sizes */
    .btn-sm {
      height: var(--height-button-sm);
      padding: var(--space-1) var(--space-3);
      font-size: var(--font-size-xs);
    }

    .btn-lg {
      height: var(--height-button-lg);
      padding: var(--space-3) var(--space-5);
      font-size: var(--font-size-base);
    }

    /* Full width */
    .btn-block {
      width: 100%;
    }

    /* Loading spinner */
    .btn-spinner {
      display: inline-block;
      width: 12px;
      height: 12px;
      border: 2px solid currentColor;
      border-right-color: transparent;
      border-radius: 50%;
      animation: spin 0.6s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `,
})
export class ButtonComponent {
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() variant: 'primary' | 'secondary' | 'ghost' | 'danger' = 'secondary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() block = false;

  get buttonClasses(): string {
    const classes = [`btn-${this.variant}`];
    if (this.size !== 'md') classes.push(`btn-${this.size}`);
    if (this.block) classes.push('btn-block');
    return classes.join(' ');
  }
}
