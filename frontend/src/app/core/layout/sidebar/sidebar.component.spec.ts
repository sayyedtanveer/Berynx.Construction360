import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../api/api-client.service';
import { SidebarComponent } from './sidebar.component';

describe('SidebarComponent licensing navigation', () => {
  async function render(modules: string[], permissions: string[]) {
    const api = {
      get: vi.fn(() => of({ modules, permissions })),
    };
    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [provideRouter([]), { provide: ApiClient, useValue: api }],
    }).compileComponents();

    const fixture = TestBed.createComponent(SidebarComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return { api, fixture, element: fixture.nativeElement as HTMLElement };
  }

  it('renders only the modules and permissions returned by the server', async () => {
    const { api, fixture, element } = await render(
      ['M02', 'M03'],
      ['users.view', 'masters.view'],
    );

    expect(api.get).toHaveBeenCalledWith('m02/context');
    expect(element.textContent).toContain('Users');
    expect(element.textContent).toContain('Products');
    const customers = Array.from(element.querySelectorAll('button')).find((button) =>
      button.textContent?.includes('Customers'),
    );
    customers?.click();
    fixture.detectChanges();
    expect(element.textContent).toContain('All Customers');
    expect(element.textContent).not.toContain('Plans & modules');
  });

  it('hides a licensed module when the user lacks its view permission', async () => {
    const { element } = await render(['M02', 'M03'], ['users.view']);

    expect(element.textContent).toContain('Users');
    expect(element.textContent).not.toContain('Products');
    expect(element.textContent).not.toContain('Customers');
  });

  it('renders M04 and M05 navigation only when the server enables those entitlements and permissions', async () => {
    const enabled = await render(
      ['M02', 'M04', 'M05'],
      ['users.view', 'm04.enquiries.view', 'm05.quotations.view'],
    );
    expect(enabled.element.textContent).toContain('Enquiries');
    expect(enabled.element.textContent).toContain('Quotations');
    enabled.fixture.destroy();
    TestBed.resetTestingModule();

    const disabled = await render(['M02'], ['m04.enquiries.view', 'm05.quotations.view']);
    expect(disabled.element.textContent).not.toContain('Enquiries');
    expect(disabled.element.textContent).not.toContain('Quotations');
  });
});
