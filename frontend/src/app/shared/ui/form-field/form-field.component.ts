import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-form-field',
  imports: [CommonModule],
  template: `
    <div [class]="fieldClasses">
      @if (label) {
        <label [for]="inputId" class="field-label">
          {{ label }}
          @if (required) {
            <span class="required-mark" aria-label="required">*</span>
          }
        </label>
      }
      <div class="field-control">
        <ng-content />
      </div>
      @if (hint && !error) {
        <p class="field-hint">{{ hint }}</p>
      }
      @if (error) {
        <p class="field-error" role="alert">{{ error }}</p>
      }
    </div>
  `,
  styles: `
    .form-field {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
    }

    .form-field-inline {
      display: grid;
      grid-template-columns: minmax(120px, 200px) 1fr;
      align-items: start;
      gap: var(--space-3);
    }

    .form-field-inline .field-label {
      padding-top: var(--space-2);
    }

    .field-label {
      display: block;
      font-size: var(--font-size-sm);
      font-weight: var(--font-weight-medium);
      color: var(--text-heading);
      line-height: var(--line-height-normal);
    }

    .required-mark {
      color: var(--danger);
      margin-left: var(--space-1);
    }

    .field-control {
      position: relative;
      width: 100%;
    }

    .field-hint {
      font-size: var(--font-size-xs);
      color: var(--text-muted);
      line-height: var(--line-height-normal);
      margin: 0;
    }

    .field-error {
      font-size: var(--font-size-xs);
      color: var(--danger);
      line-height: var(--line-height-normal);
      margin: 0;
      display: flex;
      align-items: center;
      gap: var(--space-1);
    }

    .field-error::before {
      content: '⚠';
      font-size: var(--font-size-sm);
    }

    /* Compact variant */
    .form-field-compact {
      gap: var(--space-1);
    }

    .form-field-compact .field-label {
      font-size: var(--font-size-xs);
    }
  `,
})
export class FormFieldComponent {
  @Input() label?: string;
  @Input() inputId?: string;
  @Input() hint?: string;
  @Input() error?: string;
  @Input() required = false;
  @Input() inline = false;
  @Input() compact = false;

  get fieldClasses(): string {
    const classes = ['form-field'];
    if (this.inline) classes.push('form-field-inline');
    if (this.compact) classes.push('form-field-compact');
    return classes.join(' ');
  }
}
