import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { App } from './app';
import { AuthService } from './core/auth/auth.service';

// The shell depends on AuthService (not the OIDC library directly), so a fake keeps the test
// decoupled from angular-auth-oidc-client.
const fakeAuth = {
  isAuthenticated: () => false,
  userName: () => null,
  roles: () => [],
  hasRole: () => false,
  login: () => undefined,
  logout: () => undefined,
} as unknown as AuthService;

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideNoopAnimations(), { provide: AuthService, useValue: fakeAuth }],
    }).compileComponents();
  });

  it('creates the shell', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the CompanyOps brand', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.brand-name')?.textContent).toContain('CompanyOps');
  });
});
