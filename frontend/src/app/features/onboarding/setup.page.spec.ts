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
    const client = {
      get: (path: string) => {
        calls.push(path);
        return of(path === 'auth/session' ? session : state);
      },
      post: () => of({ rows: [], difference: 0, errors: [] }),
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
    return { fixture, calls, element: fixture.nativeElement as HTMLElement };
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
});
