import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-input',
  imports: [CommonModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true,
    },
  ],
  template: `
    <input
      [type]="type"
      [placeholder]="placeholder"
      [disabled]="disabled"
      [readonly]="readonly"
      [class]="inputClasses"
      [value]="value"
      [attr.aria-invalid]="hasError"
      [attr.aria-describedby]="ariaDescribedBy"
      (input)="onInput($event)"
      (blur)="onTouched()"
    />
  `,
  styles: `
    input {
      width: 100%;
      height: var(--height-input);
      padding: var(--space-2) var(--space-3);
      font-size: var(--font-size-sm);
      font-family: inherit;
      line-height: var(--line-height-normal);
      color: var(--text-heading);
      background: var(--surface);
      border: 1px solid var(--border-default);
      border-radius: var(--radius-md);
      transition: all var(--transition-fast);
      outline: none;
    }

    input::placeholder {
      color: var(--text-disabled);
    }

    input:hover:not(:disabled):not(:readonly) {
      border-color: var(--border-medium);
    }

    input:focus {
      border-color: var(--primary-copper);
      box-shadow: 0 0 0 3px rgba(196, 106, 54, 0.1);
    }

    input:disabled {
      background: var(--canvas);
      color: var(--text-disabled);
      cursor: not-allowed;
    }

    input:readonly {
      background: var(--canvas);
    }

    input.input-error {
      border-color: var(--danger);
    }

    input.input-error:focus {
      border-color: var(--danger);
      box-shadow: 0 0 0 3px rgba(194, 65, 48, 0.1);
    }

    input.input-sm {
      height: var(--height-button-sm);
      padding: var(--space-1) var(--space-2);
      font-size: var(--font-size-xs);
    }
  `,
})
export class InputComponent implements ControlValueAccessor {
  @Input() type: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search' | 'date' | 'time' = 'text';
  @Input() placeholder = '';
  @Input() disabled = false;
  @Input() readonly = false;
  @Input() hasError = false;
  @Input() size: 'sm' | 'md' = 'md';
  @Input() ariaDescribedBy?: string;

  value = '';
  onChange: any = () => {};
  onTouched: any = () => {};

  get inputClasses(): string {
    const classes = [];
    if (this.hasError) classes.push('input-error');
    if (this.size === 'sm') classes.push('input-sm');
    return classes.join(' ');
  }

  writeValue(value: any): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.value = input.value;
    this.onChange(this.value);
  }
}
