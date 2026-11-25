import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client.service';
import { SetupPage } from './setup.page';

describe('Setup screens', () => {
  async function render(
    page: string,
    state: object,
    session: object = { authenticated: true, admin: true },
  ) {
    const calls: string[] = [];
    const requests: Array<{ method: string; path: string; body?: unknown }> = [];
    const client = {
      get: (path: string) => {
        calls.push(path);
        const data = state as Record<string, unknown>;
        if (path === 'auth/session') return of(session);
        if (path === 'platform/plans') return of(data['plans'] ?? []);
        if (path === 'platform/modules') return of(data['modules'] ?? []);
        if (path.startsWith('platform/tenants?page=')) return of(data['tenants'] ?? []);
        if (path.startsWith('platform/tenants/')) return of(data['tenantDetail'] ?? {});
        if (path.startsWith('onboarding/usage')) return of(data);
        return of(data);
      },
      post: (path: string, body?: unknown) => {
        requests.push({ method: 'POST', path, body });
        return of({ rows: [], difference: 0, errors: [], tenantId: 'tenant-1' });
      },
      put: (path: string, body?: unknown) => {
        requests.push({ method: 'PUT', path, body });
        return of({});
      },
    };
    await TestBed.configureTestingModule({
      imports: [SetupPage],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { page } } } },
        { provide: ApiClient, useValue: client },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(SetupPage);
    const initialLoad = vi.spyOn(fixture.componentInstance, 'load');
    fixture.detectChanges();
    await initialLoad.mock.results[0].value;
    await fixture.whenStable();
    fixture.detectChanges();
    return { fixture, calls, requests, element: fixture.nativeElement as HTMLElement };
  }
  it('disables go-live and links a failed check to the correct form', async () => {
    const { element } = await render('checklist', {
      checks: [{ label: 'Bank account', route: 'accounts', passed: false }],
    });
    expect(element.querySelector<HTMLButtonElement>('button')?.disabled).toBe(true);
    expect(element.querySelector('a[href="/setup/accounts"]')?.textContent).toContain(
      'Bank account',
    );
  });
  it('resumes the server progress and displays the read-only banner', async () => {
    const { element } = await render('wizard', { progress: 50, status: 'Suspended' });
    expect(element.querySelector('progress')?.value).toBe(50);
    expect(element.textContent).toContain('Read-only');
  });
  it('does not request the platform list for a tenant principal', async () => {
    const { element, calls } = await render('platform', {});
    expect(calls).toEqual(['auth/session']);
    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Platform administrator',
    );
  });
  it('loads the platform tenant workspace only for a PlatformAdmin session', async () => {
    const { calls, element } = await render(
      'platform',
      { plans: [], modules: [], tenants: [] },
      { authenticated: true, platform: true },
    );
    expect(calls).toEqual([
      'auth/session',
      'platform/plans',
      'platform/modules',
      'platform/tenants?page=1',
    ]);
    expect(element.textContent).toContain('Create tenant');
    expect(element.textContent).toContain('Create a plan');
  });
  it('includes the monthly API request quota when creating a plan', async () => {
    const { fixture, requests } = await render(
      'plans',
      {
        plans: [],
        modules: [
          { moduleCode: 'M01', name: 'Onboarding', isCore: true, isActive: true },
          { moduleCode: 'M02', name: 'Identity', isCore: true, isActive: true },
        ],
      },
      { authenticated: true, platform: true },
    );
    fixture.componentInstance.planForm['planCode'] = 'launch';
    fixture.componentInstance.planForm['name'] = 'Launch';
    fixture.componentInstance.planForm['maxUsers'] = 10;
    fixture.componentInstance.planForm['maxStorageBytes'] = 100000;
    fixture.componentInstance.planForm['maxDocumentsPerMonth'] = 1000;
    fixture.componentInstance.planForm['maxMessagesPerMonth'] = 100;
    fixture.componentInstance.planForm['maxApiRequestsPerMonth'] = 50000;
    fixture.componentInstance.planForm['trialDays'] = 14;
    await fixture.componentInstance.savePlan();

    expect(requests.find((request) => request.path === 'platform/plans')?.body).toMatchObject({
      maxApiRequestsPerMonth: 50000,
    });
  });
});
