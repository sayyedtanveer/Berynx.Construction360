import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { catchError, of } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';

export type M03ContextResource = 'customer' | 'site' | 'contact' | 'supplier' | 'product';
export type M03ContextFieldType = 'text' | 'email' | 'number' | 'date' | 'select' | 'checkbox' | 'json';
export interface M03ContextField {
  key: string;
  label: string;
  type?: M03ContextFieldType;
  required?: boolean;
  min?: number;
  options?: string[];
}

const RESOURCE_CONFIG: Record<M03ContextResource, {
  title: string;
  endpoint: string;
  fields: M03ContextField[];
}> = {
  customer: {
    title: 'Customer',
    endpoint: '/m03/parties',
    fields: [
      { key: 'legalName', label: 'Legal name', required: true },
      { key: 'partyType', label: 'Party type', type: 'select', required: true, options: ['Business', 'Individual', 'Institution'] },
      { key: 'gstin', label: 'GSTIN' },
      { key: 'city', label: 'City' },
      { key: 'stateCode', label: 'GST state code' },
      { key: 'creditLimit', label: 'Credit limit', type: 'number', required: true, min: 0 },
      { key: 'creditDays', label: 'Credit days', type: 'number', required: true, min: 0 },
      { key: 'creditPolicy', label: 'Credit policy', type: 'select', required: true, options: ['Warn', 'SoftBlock', 'HardBlock'] },
      { key: 'creditStatus', label: 'Credit status', type: 'select', required: true, options: ['Active', 'OnHold', 'Blocked'] },
    ],
  },
  site: {
    title: 'Customer site',
    endpoint: '/m03/sites',
    fields: [
      { key: 'name', label: 'Site name', required: true },
      { key: 'address', label: 'Address', required: true },
      { key: 'city', label: 'City', required: true },
      { key: 'stateCode', label: 'GST state code', required: true },
    ],
  },
  contact: {
    title: 'Contact',
    endpoint: '/m03/contacts',
    fields: [
      { key: 'name', label: 'Name', required: true },
      { key: 'role', label: 'Role', required: true },
      { key: 'email', label: 'Email', type: 'email' },
      { key: 'mobile', label: 'Mobile (+country code)' },
    ],
  },
  supplier: {
    title: 'Supplier',
    endpoint: '/m03/parties',
    fields: [
      { key: 'legalName', label: 'Legal name', required: true },
      { key: 'partyType', label: 'Party type', type: 'select', required: true, options: ['Business', 'Individual', 'Institution'] },
      { key: 'gstin', label: 'GSTIN' },
      { key: 'city', label: 'City' },
      { key: 'stateCode', label: 'GST state code' },
      { key: 'isMsme', label: 'MSME / Udyam', type: 'checkbox' },
      { key: 'udyamNumber', label: 'Udyam number' },
      { key: 'tdsCategory', label: 'TDS category' },
      { key: 'creditLimit', label: 'Credit limit', type: 'number', required: true, min: 0 },
      { key: 'creditDays', label: 'Credit days', type: 'number', required: true, min: 0 },
      { key: 'creditPolicy', label: 'Credit policy', type: 'select', required: true, options: ['Warn', 'SoftBlock', 'HardBlock'] },
      { key: 'creditStatus', label: 'Credit status', type: 'select', required: true, options: ['Active', 'OnHold', 'Blocked'] },
    ],
  },
  product: {
    title: 'Product',
    endpoint: '/m03/products',
    fields: [
      { key: 'code', label: 'Product code', required: true },
      { key: 'name', label: 'Product name', required: true },
      { key: 'category', label: 'Category', required: true },
      { key: 'hsnCode', label: 'HSN code', required: true },
      { key: 'baseUnitCode', label: 'Base unit code', required: true },
      { key: 'categoryAttributesJson', label: 'Category attributes (JSON)', type: 'json', required: true },
      { key: 'qualityReference', label: 'Quality reference' },
      { key: 'traceable', label: 'Traceable', type: 'checkbox' },
    ],
  },
};

@Component({
  selector: 'app-m03-create-in-context-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (open) {
      <div class="backdrop" (click)="close()" aria-hidden="true"></div>
      <aside class="drawer" role="dialog" aria-modal="true" [attr.aria-label]="'Create ' + config.title">
        <header>
          <h2>Create {{ config.title }}</h2>
          <button type="button" class="close" (click)="close()" aria-label="Close">×</button>
        </header>
        <p class="hint">The parent form remains open. The new record will be selected after it is saved.</p>
        @if (error) { <p role="alert" class="error">{{ error }}</p> }
        <form (ngSubmit)="save()">
          @for (field of config.fields; track field.key) {
            @if (field.type === 'checkbox') {
              <label class="check-field">
                <input type="checkbox" [name]="field.key" [(ngModel)]="values[field.key]" />
                {{ field.label }}
              </label>
            } @else if (field.type === 'select') {
              <label>{{ field.label }}
                <select [name]="field.key" [(ngModel)]="values[field.key]" [required]="!!field.required">
                  <option value="">Select…</option>
                  @for (option of field.options ?? []; track option) { <option [value]="option">{{ option }}</option> }
                </select>
              </label>
            } @else if (field.type === 'json') {
              <label>{{ field.label }}
                <textarea [name]="field.key" [(ngModel)]="values[field.key]" [required]="!!field.required" rows="4"></textarea>
              </label>
            } @else {
              <label>{{ field.label }}
                <input [type]="field.type ?? 'text'" [name]="field.key"
                  [(ngModel)]="values[field.key]" [required]="!!field.required" [min]="field.min ?? null" />
              </label>
            }
          }
          <footer>
            <button type="button" class="secondary" (click)="close()">Cancel</button>
            <button type="submit" [disabled]="saving">{{ saving ? 'Saving…' : 'Save and select' }}</button>
          </footer>
        </form>
      </aside>
    }
  `,
  styles: `
    :host { display: contents; }
    .backdrop { background: rgb(15 24 22 / 42%); inset: 0; position: fixed; z-index: 20; }
    .drawer { background: var(--surface, white); box-shadow: 0 1rem 3rem rgb(0 0 0 / 20%); height: 100dvh; inset: 0 0 0 auto; max-width: 32rem; overflow-y: auto; padding: 1.5rem; position: fixed; width: min(100%, 32rem); z-index: 21; }
    header, footer { align-items: center; display: flex; gap: .75rem; justify-content: space-between; }
    h2 { margin: 0; }
    .hint { color: var(--muted); line-height: 1.5; }
    form { display: grid; gap: 1rem; }
    label { display: grid; font-weight: 650; gap: .4rem; }
    input { background: var(--surface, white); border: 1px solid var(--border); border-radius: .55rem; color: var(--text); font: inherit; min-height: 2.7rem; padding: .55rem .7rem; }
    select { background: var(--surface, white); border: 1px solid var(--border); border-radius: .55rem; color: var(--text); font: inherit; min-height: 2.7rem; padding: .55rem .7rem; }
    textarea { background: var(--surface, white); border: 1px solid var(--border); border-radius: .55rem; color: var(--text); font: inherit; min-height: 6rem; padding: .55rem .7rem; }
    .check-field { align-items: center; display: flex; gap: .6rem; }
    button { background: var(--accent); border: 1px solid var(--accent); border-radius: .55rem; color: white; cursor: pointer; font: inherit; font-weight: 650; min-height: 2.7rem; padding: .5rem .9rem; }
    button:disabled { cursor: wait; opacity: .65; }
    button.secondary, button.close { background: transparent; color: var(--accent); }
    button.close { border: 0; font-size: 1.5rem; min-height: 2rem; padding: .25rem .5rem; }
    .error { background: #fff1ed; border-radius: .5rem; color: #8a2d19; padding: .75rem; }
    footer { justify-content: end; margin-top: .5rem; }
  `,
})
export class M03CreateInContextDrawerComponent {
  private readonly api = inject(ApiClient);

  @Input({ required: true }) resource!: M03ContextResource;
  @Input() open = false;
  @Input() context: Record<string, unknown> = {};
  @Output() readonly created = new EventEmitter<Record<string, unknown>>();
  @Output() readonly closed = new EventEmitter<void>();

  values: Record<string, unknown> = {};
  saving = false;
  error = '';

  get config(): typeof RESOURCE_CONFIG[M03ContextResource] {
    return RESOURCE_CONFIG[this.resource];
  }

  save(): void {
    if (this.saving) return;
    this.error = '';
    const payload = { ...this.context, ...this.values };
    if (this.resource === 'customer' || this.resource === 'supplier') {
      Object.assign(payload, {
        roles: [this.resource === 'customer' ? 'Customer' : 'Supplier'],
        version: 0,
      });
    } else if (this.resource === 'product') {
      Object.assign(payload, {
        version: 0,
      });
    } else {
      Object.assign(payload, { version: 0 });
      if (this.resource === 'site') {
        payload['status'] = 'Active';
        payload['accessNotes'] ??= '';
      }
    }

    this.saving = true;
    let failed = false;
    this.api.post<Record<string, unknown>, unknown>(this.config.endpoint, payload).pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        failed = true;
        this.error = error.error?.detail ?? error.error?.title ??
          'The record could not be saved. Check permissions, validation and network connectivity.';
        this.saving = false;
        return of(null);
      }),
    ).subscribe((response) => {
      this.saving = false;
      if (failed || !response || typeof response !== 'object') return;
      this.created.emit(response as Record<string, unknown>);
      this.values = {};
      this.close();
    });
  }

  close(): void {
    this.open = false;
    this.error = '';
    this.closed.emit();
  }
}
