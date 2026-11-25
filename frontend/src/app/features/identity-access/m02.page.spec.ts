import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { M02Page } from './m02.page';

describe('M02 users page', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [M02Page],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { data: { screen: 'users' } },
          },
        },
      ],
    }).compileComponents();
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function loadUsersPage(permissions: string[]) {
    const fixture = TestBed.createComponent(M02Page);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/v1/m02/context').flush({ permissions, scopeTypes: ['All'] });
    http.expectOne('/api/v1/m02/users').flush([]);
    http.expectOne('/api/v1/m02/roles').flush({
      roles: [{
        roleId: 'role-1', roleKey: 'SalesManager', name: 'Sales Manager', description: '',
        isSystem: true, version: 1, permissionCodes: [],
      }],
    });
    http.expectOne('/api/v1/m02/scopes').flush({ branches: [], plants: [] });
    fixture.detectChanges();
    return { fixture, http };
  }

  it('does not show Add User controls without users.create permission', () => {
    const { fixture } = loadUsersPage(['users.view']);
    const page = fixture.nativeElement as HTMLElement;
    expect(page.textContent).toContain('does not have permission to add or edit tenant users');
    expect(page.querySelector('form.user-form')).toBeNull();
  });

  it('submits a new user through the authenticated M02 invitation API', () => {
    const { fixture, http } = loadUsersPage(['users.view', 'users.create']);
    const component = fixture.componentInstance;
    expect(component.can('users.create')).toBe(true);
    component.userForm = {
      displayName: 'Branch Sales',
      email: 'branch.sales@example.invalid',
      mobile: '+919876543210',
    };
    component.inviteUser();

    const request = http.expectOne('/api/v1/m02/users');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toMatchObject({
      displayName: 'Branch Sales',
      email: 'branch.sales@example.invalid',
      mobile: '+919876543210',
      roles: [{ roleKey: 'SalesManager', scopeType: 'All', scopeId: null }],
    });
    request.flush({ userId: 'new-user', inviteExpiresAt: '2027-01-01T00:00:00Z' });
    http.expectOne('/api/v1/m02/users').flush([]);
    http.expectOne('/api/v1/m02/context').flush({ permissions: ['users.view', 'users.create'], scopeTypes: ['All'] });
    http.expectOne('/api/v1/m02/roles').flush({ roles: [] });
    http.expectOne('/api/v1/m02/scopes').flush({ branches: [], plants: [] });
    fixture.detectChanges();
    expect(component.can('users.create')).toBe(true);
    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('Invitation queued. The link expires in 72 hours.');
  });
});
