import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClient } from '../../core/api/api-client.service';

interface ProductOption {
  productId: string;
  code: string;
  name: string;
  baseUnitCode: string;
}

interface ContactOption {
  contactId: string;
  name: string;
  role?: string;
}

interface CatalogueItem {
  catalogueItemId: string;
  productId: string;
  code: string;
  name: string;
  baseUnitCode: string;
  description: string;
  imageReference: string;
  keySpecs: string;
  indicativePrice: number | null;
  isPublished: boolean;
  updatedAt: string;
}

interface CatalogueRequest {
  productId: string;
  description: string;
  imageReference: string;
  keySpecs: string;
  indicativePrice: number | null;
  isPublished: boolean;
}

interface ConsentRecord {
  consentId: string;
  contactId: string;
  channel: string;
  isOptedIn: boolean;
  capturedAt: string;
  source: string;
  wording: string;
  optedOutAt: string | null;
  optOutSource: string;
}

interface ConsentRequest {
  contactId: string;
  channel: string;
  isOptedIn: boolean;
  source: string;
  wording: string;
}

@Component({
  selector: 'app-m17-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="workspace">
      <header class="page-head">
        <div>
          <p class="eyebrow">GROWTH · M17 PROMOTION & CATALOGUE</p>
          <h1>Promotion & catalogue</h1>
          <p class="intro">Manage published product information and auditable customer marketing consent.</p>
        </div>
        <button type="button" class="secondary" (click)="reload()" [disabled]="loading">Refresh</button>
      </header>

      <p class="notice error" role="alert" *ngIf="error">{{ error }}</p>
      <p class="notice success" role="status" *ngIf="success">{{ success }}</p>

      <nav class="tabs" aria-label="Promotion workspace">
        <button type="button" [class.active]="activeTab === 'catalogue'" (click)="selectTab('catalogue')">Catalogue</button>
        <button type="button" [class.active]="activeTab === 'consents'" (click)="selectTab('consents')">Consent records</button>
      </nav>

      <section *ngIf="activeTab === 'catalogue'" aria-labelledby="catalogue-heading">
        <div class="section-head">
          <div><p class="eyebrow">TENANT CATALOGUE</p><h2 id="catalogue-heading">Product entries</h2></div>
          <span class="count">{{ publishedCount }} published · {{ catalogue.length }} total</span>
        </div>

        <form class="editor" (ngSubmit)="saveCatalogue()">
          <h3>{{ editingCatalogue ? 'Update catalogue entry' : 'Add product to catalogue' }}</h3>
          <label>Product
            <select name="productId" [(ngModel)]="catalogueDraft.productId" required>
              <option value="">Select an active product</option>
              <option *ngFor="let product of products" [value]="product.productId">{{ product.code }} · {{ product.name }} ({{ product.baseUnitCode }})</option>
            </select>
          </label>
          <label>Description
            <textarea name="description" [(ngModel)]="catalogueDraft.description" maxlength="2000" rows="2"></textarea>
          </label>
          <div class="fields">
            <label>Image reference
              <input name="imageReference" [(ngModel)]="catalogueDraft.imageReference" maxlength="500" autocomplete="off">
            </label>
            <label>Key specifications
              <input name="keySpecs" [(ngModel)]="catalogueDraft.keySpecs" maxlength="2000" autocomplete="off">
            </label>
            <label>Indicative price
              <input name="indicativePrice" type="number" min="0" step="0.01" [(ngModel)]="catalogueDraft.indicativePrice">
            </label>
          </div>
          <label class="check"><input name="isPublished" type="checkbox" [(ngModel)]="catalogueDraft.isPublished"> Publish in the tenant's public catalogue</label>
          <div class="actions">
            <button type="submit" [disabled]="saving || !catalogueDraft.productId">{{ saving ? 'Saving…' : 'Save entry' }}</button>
            <button type="button" class="secondary" (click)="resetCatalogue()" [disabled]="saving">Clear form</button>
          </div>
          <p class="hint">Indicative prices are informational; commercial offers and accepted quotations remain the source of truth.</p>
        </form>

        <div class="table-tools">
          <label class="search">Search catalogue
            <input name="catalogueSearch" [(ngModel)]="catalogueSearch" placeholder="Product or description">
          </label>
        </div>
        <p *ngIf="loadingCatalogue" class="muted" role="status">Loading catalogue…</p>
        <div class="table-wrap" *ngIf="!loadingCatalogue">
          <table>
            <thead><tr><th>Product</th><th>Description</th><th>Indicative price</th><th>Publication</th><th>Last updated</th><th>Action</th></tr></thead>
            <tbody>
              <tr *ngFor="let item of filteredCatalogue">
                <td><strong>{{ item.name }}</strong><small>{{ item.code }} · {{ item.baseUnitCode }}</small></td>
                <td>{{ item.description || '—' }}<small *ngIf="item.keySpecs">{{ item.keySpecs }}</small></td>
                <td>{{ item.indicativePrice === null ? '—' : (item.indicativePrice | number:'1.2-2') }}</td>
                <td><span class="status" [class.published]="item.isPublished">{{ item.isPublished ? 'Published' : 'Draft' }}</span></td>
                <td>{{ item.updatedAt | date:'medium' }}</td>
                <td><button type="button" class="text-button" (click)="editCatalogue(item)">Edit</button></td>
              </tr>
              <tr *ngIf="filteredCatalogue.length === 0"><td colspan="6" class="empty">{{ catalogue.length ? 'No catalogue entries match this search.' : 'No catalogue entries yet. Add an active product above.' }}</td></tr>
            </tbody>
          </table>
        </div>
      </section>

      <section *ngIf="activeTab === 'consents'" aria-labelledby="consents-heading">
        <div class="section-head">
          <div><p class="eyebrow">AUDITABLE CONTACT PREFERENCES</p><h2 id="consents-heading">Marketing consent</h2></div>
        </div>
        <form class="editor consent-editor" (ngSubmit)="saveConsent()">
          <h3>Record consent or opt-out</h3>
          <div class="fields">
            <label>Contact
              <select name="contactId" [(ngModel)]="consentDraft.contactId" required>
                <option value="">Select a contact</option>
                <option *ngFor="let contact of contacts" [value]="contact.contactId">{{ contact.name }}{{ contact.role ? ' · ' + contact.role : '' }}</option>
              </select>
            </label>
            <label>Channel
              <select name="channel" [(ngModel)]="consentDraft.channel" required>
                <option value="email">Email</option><option value="sms">SMS</option><option value="whatsapp">WhatsApp</option>
              </select>
            </label>
            <label>Preference
              <select name="isOptedIn" [(ngModel)]="consentDraft.isOptedIn">
                <option [ngValue]="true">Opted in</option><option [ngValue]="false">Opted out</option>
              </select>
            </label>
            <label>Capture source
              <input name="source" [(ngModel)]="consentDraft.source" maxlength="120" required>
            </label>
          </div>
          <label>Exact wording shown to the contact
            <textarea name="wording" [(ngModel)]="consentDraft.wording" maxlength="1000" rows="2" required></textarea>
          </label>
          <button type="submit" [disabled]="savingConsent || !consentDraft.contactId || !consentDraft.source.trim() || !consentDraft.wording.trim()">{{ savingConsent ? 'Saving…' : 'Save consent record' }}</button>
        </form>
        <div class="table-tools">
          <label class="search">Search consent records
            <input name="consentSearch" [(ngModel)]="consentSearch" placeholder="Contact or channel">
          </label>
          <button type="button" class="secondary" (click)="loadConsents()" [disabled]="loadingConsents">Refresh records</button>
        </div>
        <p *ngIf="loadingConsents" class="muted" role="status">Loading consent records…</p>
        <div class="table-wrap" *ngIf="!loadingConsents">
          <table>
            <thead><tr><th>Contact</th><th>Channel</th><th>Preference</th><th>Captured</th><th>Source and wording</th></tr></thead>
            <tbody>
              <tr *ngFor="let record of filteredConsents">
                <td>{{ contactName(record.contactId) }}</td><td>{{ record.channel }}</td>
                <td><span class="status" [class.published]="record.isOptedIn">{{ record.isOptedIn ? 'Opted in' : 'Opted out' }}</span></td>
                <td>{{ record.capturedAt | date:'medium' }}</td><td>{{ record.source }}<small>{{ record.wording }}</small></td>
              </tr>
              <tr *ngIf="filteredConsents.length === 0"><td colspan="5" class="empty">{{ consents.length ? 'No consent records match this search.' : 'No consent records are available.' }}</td></tr>
            </tbody>
          </table>
        </div>
      </section>
    </main>
  `,
  styles: [`
    :host{display:block;color:#252d2b}.workspace{max-width:1440px;margin:auto;padding:32px clamp(16px,3vw,40px) 56px}.page-head,.section-head,.table-tools,.actions{display:flex;align-items:center;justify-content:space-between;gap:16px}.page-head{margin-bottom:24px}.eyebrow{font-size:11px;letter-spacing:.12em;font-weight:700;color:#9b6945;margin:0 0 8px}h1{font-size:30px;margin:0;color:#173a35}h2{font-size:22px;margin:0;color:#173a35}h3{font-size:16px;color:#173a35;margin:0 0 14px}.intro,.hint,.muted{color:#68736f}.intro{margin:8px 0 0}.tabs{display:flex;gap:8px;border-bottom:1px solid #d8dfdc;margin:0 0 24px}.tabs button,.secondary{background:#fff;border:1px solid #cad4d0;color:#29413b;border-radius:8px;padding:9px 14px}.tabs button{border:0;border-radius:0}.tabs button.active{color:#9b6945;border-bottom:2px solid #9b6945;font-weight:700}.section-head{margin:18px 0}.count{font-size:13px;color:#68736f}.editor{background:#fff;border:1px solid #dce3df;border-radius:12px;padding:20px;margin:16px 0 22px;display:grid;gap:14px;box-shadow:0 4px 18px #173a3508}.fields{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:14px}label{display:grid;gap:6px;font-size:13px;font-weight:600;color:#344b45}input,select,textarea{font:inherit;border:1px solid #cbd5d1;border-radius:7px;padding:9px 10px;color:#263a35;background:#fff;min-width:0}textarea{resize:vertical}.check{display:flex;align-items:center;gap:8px}.check input{min-width:auto}.actions{justify-content:flex-start}.actions button,form>button[type=submit],.page-head>button:not(.secondary){background:#173a35;color:white;border:0;border-radius:8px;padding:10px 16px;font-weight:700}.actions button:disabled,form>button:disabled,.page-head button:disabled{opacity:.55;cursor:not-allowed}.hint{font-size:12px;margin:0}.table-tools{margin:10px 0}.search{display:flex;align-items:center;gap:10px}.search input{min-width:240px}.table-wrap{overflow:auto;border:1px solid #dce3df;border-radius:10px;background:#fff}table{border-collapse:collapse;width:100%;min-width:760px}th,td{text-align:left;padding:12px 14px;border-bottom:1px solid #edf0ee;vertical-align:top}th{font-size:11px;text-transform:uppercase;letter-spacing:.06em;color:#66746f;background:#f7f8f7}td{font-size:13px}td small{display:block;color:#71807a;margin-top:4px;max-width:380px;overflow-wrap:anywhere}.status{display:inline-block;border-radius:999px;background:#f1ece6;color:#805d3d;padding:4px 9px;font-size:12px}.status.published{background:#e5f1e8;color:#286641}.text-button{border:0;background:transparent;color:#895b39;font-weight:700;cursor:pointer}.empty{text-align:center;padding:28px;color:#71807a}.notice{border-radius:8px;padding:11px 14px;margin:12px 0}.error{background:#fff0ed;color:#9d3528}.success{background:#e8f4eb;color:#286641}@media(max-width:680px){.workspace{padding-top:20px}.page-head{align-items:flex-start}.page-head h1{font-size:24px}.table-tools{align-items:flex-start;flex-direction:column}.search{align-items:flex-start;flex-direction:column;width:100%}.search input{width:100%;min-width:0}.editor{padding:15px}}
  `],
})
export class M17Page implements OnInit {
  private readonly api = inject(ApiClient);

  products: ProductOption[] = [];
  contacts: ContactOption[] = [];
  catalogue: CatalogueItem[] = [];
  consents: ConsentRecord[] = [];
  activeTab: 'catalogue' | 'consents' = 'catalogue';
  loading = false;
  loadingCatalogue = false;
  loadingConsents = false;
  saving = false;
  savingConsent = false;
  error = '';
  success = '';
  catalogueSearch = '';
  consentSearch = '';
  editingCatalogue = false;
  catalogueDraft: CatalogueRequest = this.emptyCatalogue();
  consentDraft: ConsentRequest = this.emptyConsent();

  ngOnInit(): void {
    this.reload();
  }

  get publishedCount(): number {
    return this.catalogue.filter((item) => item.isPublished).length;
  }

  get filteredCatalogue(): CatalogueItem[] {
    const term = this.catalogueSearch.trim().toLowerCase();
    if (!term) return this.catalogue;
    return this.catalogue.filter((item) =>
      `${item.code} ${item.name} ${item.description} ${item.keySpecs}`.toLowerCase().includes(term),
    );
  }

  get filteredConsents(): ConsentRecord[] {
    const term = this.consentSearch.trim().toLowerCase();
    if (!term) return this.consents;
    return this.consents.filter((record) =>
      `${this.contactName(record.contactId)} ${record.channel} ${record.source} ${record.wording}`
        .toLowerCase()
        .includes(term),
    );
  }

  reload(): void {
    this.error = '';
    this.loading = true;
    let pending = 2;
    const finish = (): void => {
      pending -= 1;
      if (pending === 0) this.loading = false;
    };
    this.api.get<ProductOption[]>('/m03/products').subscribe({
      next: (rows) => { this.products = rows; finish(); },
      error: () => { this.error = 'Products could not be loaded. Check master-data access and try again.'; finish(); },
    });
    this.loadCatalogue(finish);
    this.api.get<ContactOption[]>('/m03/contacts').subscribe({
      next: (rows) => { this.contacts = rows; },
      error: () => { this.error = this.error || 'Contacts could not be loaded. Consent capture may be unavailable.'; },
    });
  }

  selectTab(tab: 'catalogue' | 'consents'): void {
    this.activeTab = tab;
    this.success = '';
    if (tab === 'consents' && this.consents.length === 0) this.loadConsents();
  }

  loadConsents(): void {
    this.error = '';
    this.loadingConsents = true;
    this.api.get<ConsentRecord[]>('/m17/consents').subscribe({
      next: (rows) => { this.consents = rows; this.loadingConsents = false; },
      error: () => { this.error = 'Consent records could not be loaded. The signed-in user needs M17 consent-view permission.'; this.loadingConsents = false; },
    });
  }

  editCatalogue(item: CatalogueItem): void {
    this.editingCatalogue = true;
    this.catalogueDraft = {
      productId: item.productId,
      description: item.description,
      imageReference: item.imageReference,
      keySpecs: item.keySpecs,
      indicativePrice: item.indicativePrice,
      isPublished: item.isPublished,
    };
    this.success = '';
  }

  resetCatalogue(): void {
    this.catalogueDraft = this.emptyCatalogue();
    this.editingCatalogue = false;
  }

  saveCatalogue(): void {
    if (!this.catalogueDraft.productId || this.saving) return;
    this.error = '';
    this.success = '';
    this.saving = true;
    const request = { ...this.catalogueDraft };
    this.api.post<unknown, CatalogueRequest>('/m17/catalogue', request).subscribe({
      next: () => {
        this.saving = false;
        this.success = this.editingCatalogue ? 'Catalogue entry updated.' : 'Catalogue entry saved.';
        this.resetCatalogue();
        this.loadCatalogue();
      },
      error: () => {
        this.saving = false;
        this.error = 'Catalogue entry was not saved. Check M17 permission, product status, and module entitlement.';
      },
    });
  }

  saveConsent(): void {
    if (!this.consentDraft.contactId || !this.consentDraft.source.trim() || !this.consentDraft.wording.trim() || this.savingConsent) return;
    this.error = '';
    this.success = '';
    this.savingConsent = true;
    const request = { ...this.consentDraft, source: this.consentDraft.source.trim(), wording: this.consentDraft.wording.trim() };
    this.api.post<unknown, ConsentRequest>('/m17/consents', request).subscribe({
      next: () => {
        this.savingConsent = false;
        this.success = 'Consent record saved.';
        this.consentDraft = this.emptyConsent();
        this.loadConsents();
      },
      error: () => {
        this.savingConsent = false;
        this.error = 'Consent was not saved. Check the contact, exact wording, M17 permission, and module entitlement.';
      },
    });
  }

  contactName(contactId: string): string {
    return this.contacts.find((contact) => contact.contactId === contactId)?.name ?? contactId;
  }

  private loadCatalogue(done?: () => void): void {
    this.loadingCatalogue = true;
    this.api.get<CatalogueItem[]>('/m17/catalogue').subscribe({
      next: (rows) => { this.catalogue = rows; this.loadingCatalogue = false; done?.(); },
      error: () => { this.error = this.error || 'Catalogue could not be loaded. Check M17 catalogue permission and module entitlement.'; this.loadingCatalogue = false; done?.(); },
    });
  }

  private emptyCatalogue(): CatalogueRequest {
    return { productId: '', description: '', imageReference: '', keySpecs: '', indicativePrice: null, isPublished: false };
  }

  private emptyConsent(): ConsentRequest {
    return { contactId: '', channel: 'email', isOptedIn: true, source: '', wording: '' };
  }
}