import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { M17Page } from './m17.page';

describe('M17Page', () => {
  it('loads catalogue and product options from their API contracts', () => {
    const catalogue = [{
      catalogueItemId: 'catalogue-1', productId: 'product-1', code: 'RM-1', name: 'Ready mix',
      baseUnitCode: 'M3', description: 'Concrete', imageReference: '', keySpecs: '',
      indicativePrice: null, isPublished: false, updatedAt: '2026-01-01T00:00:00Z',
    }];
    const api = {
      get: vi.fn((path: string) => of(path === '/m17/catalogue' ? catalogue : [])),
      post: vi.fn(() => of({})),
    };
    TestBed.configureTestingModule({
      imports: [M17Page],
      providers: [{ provide: ApiClient, useValue: api }],
    });

    const fixture = TestBed.createComponent(M17Page);
    fixture.detectChanges();

    expect(api.get).toHaveBeenCalledWith('/m17/catalogue');
    expect(api.get).toHaveBeenCalledWith('/m03/products');
    expect(fixture.componentInstance.catalogue).toEqual(catalogue);
    expect(fixture.componentInstance.filteredCatalogue[0].name).toBe('Ready mix');
  });

  it('saves a catalogue entry through the API and refreshes persisted rows', () => {
    const api = {
      get: vi.fn(() => of([])),
      post: vi.fn(() => of({ catalogueItemId: 'catalogue-1' })),
    };
    TestBed.configureTestingModule({
      imports: [M17Page],
      providers: [{ provide: ApiClient, useValue: api }],
    });
    const fixture = TestBed.createComponent(M17Page);
    fixture.detectChanges();
    const page = fixture.componentInstance;
    const request = {
      productId: 'product-1', description: 'Concrete', imageReference: '',
      keySpecs: 'Grade 30', indicativePrice: 0, isPublished: true,
    };
    page.catalogueDraft = request;

    page.saveCatalogue();

    expect(api.post).toHaveBeenCalledWith('/m17/catalogue', request);
    expect(api.get).toHaveBeenCalledTimes(4);
    expect(page.success).toBe('Catalogue entry saved.');
    expect(page.saving).toBe(false);
  });

  it('does not report successful consent capture when the API fails', () => {
    const api = {
      get: vi.fn(() => of([])),
      post: vi.fn(() => throwError(() => new Error('request failed'))),
    };
    TestBed.configureTestingModule({
      imports: [M17Page],
      providers: [{ provide: ApiClient, useValue: api }],
    });
    const fixture = TestBed.createComponent(M17Page);
    fixture.detectChanges();
    const page = fixture.componentInstance;
    page.consentDraft = {
      contactId: 'contact-1', channel: 'email', isOptedIn: true,
      source: 'Web form', wording: 'I agree to receive product updates.',
    };

    page.saveConsent();
    expect(page.success).toBe('');
    expect(page.error).toContain('Consent was not saved');
    expect(page.savingConsent).toBe(false);
  });
});