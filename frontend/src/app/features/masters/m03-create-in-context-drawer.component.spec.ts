import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';
import { M03CreateInContextDrawerComponent } from './m03-create-in-context-drawer.component';

describe('M03CreateInContextDrawerComponent', () => {
  it('creates and emits a customer with the parent context and expected role', async () => {
    const api = { post: vi.fn(() => of({ partyId: 'party-new', legalName: 'New customer' })) };
    await TestBed.configureTestingModule({
      imports: [M03CreateInContextDrawerComponent],
      providers: [
        provideHttpClient(),
        { provide: ApiClient, useValue: api },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(M03CreateInContextDrawerComponent);
    fixture.componentInstance.resource = 'customer';
    fixture.componentInstance.context = { sourceFormValue: 'preserve-me' };
    fixture.componentInstance.open = true;
    fixture.componentInstance.values = {
      legalName: 'New customer', partyType: 'Business', creditLimit: 0,
      creditDays: 0, creditPolicy: 'Warn', creditStatus: 'Active',
    };
    const created = vi.fn();
    fixture.componentInstance.created.subscribe(created);
    fixture.detectChanges();

    fixture.componentInstance.save();
    await fixture.whenStable();

    expect(api.post).toHaveBeenCalledWith('/m03/parties', expect.objectContaining({
      legalName: 'New customer',
      partyType: 'Business',
      sourceFormValue: 'preserve-me',
      creditLimit: 0,
      creditDays: 0,
      creditPolicy: 'Warn',
      roles: ['Customer'],
      creditStatus: 'Active',
    }));
    expect(created).toHaveBeenCalledWith({ partyId: 'party-new', legalName: 'New customer' });
  });

  it('surfaces server validation and permission errors without closing the drawer', async () => {
    const api = {
      post: vi.fn(() => throwError(() => ({ error: { detail: 'Permission denied.' } }))),
    };
    await TestBed.configureTestingModule({
      imports: [M03CreateInContextDrawerComponent],
      providers: [
        provideHttpClient(),
        { provide: ApiClient, useValue: api },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(M03CreateInContextDrawerComponent);
    fixture.componentInstance.resource = 'product';
    fixture.componentInstance.open = true;
    fixture.componentInstance.values = {
      code: 'P-1', name: 'Product', category: 'Other', hsnCode: '6810', baseUnitCode: 'EA',
    };
    fixture.detectChanges();
    fixture.componentInstance.save();
    fixture.detectChanges();

    expect(fixture.componentInstance.error).toBe('Permission denied.');
    expect(fixture.componentInstance.open).toBe(true);
  });
});
