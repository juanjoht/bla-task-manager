import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  const isAuthenticated = signal(false);

  function runGuard() {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: { isAuthenticated } }],
    });
    return TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
  }

  it('lets a logged-in user through', () => {
    isAuthenticated.set(true);

    expect(runGuard()).toBe(true);
  });

  it('sends a logged-out user to the login page', () => {
    isAuthenticated.set(false);

    const result = runGuard();

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });
});
