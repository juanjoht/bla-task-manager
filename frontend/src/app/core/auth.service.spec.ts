import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthResponse } from '../shared/models/auth.model';
import { AuthService } from './auth.service';

const API = 'http://localhost:5276/api';
const demoUser = { id: '1', email: 'demo@ballastlane.test', name: 'Demo User' };

/** A JWT-shaped token whose payload expires the given number of seconds from now (signature is irrelevant here). */
function fakeJwt(expiresInSeconds: number): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
  const exp = Math.floor(Date.now() / 1000) + expiresInSeconds;
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({ sub: '1', exp })}.signature`;
}

function storeSession(token: string) {
  localStorage.setItem('taskManager.session', JSON.stringify({ token, user: demoUser }));
}

const authResponse: AuthResponse = { token: fakeJwt(3600), user: demoUser };

describe('AuthService', () => {
  let http: HttpTestingController;

  function createService(): AuthService {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  beforeEach(() => localStorage.clear());
  afterEach(() => http.verify());

  it('starts logged out', () => {
    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('logs in: posts the credentials, then keeps the token and the user', () => {
    const service = createService();

    service.login({ email: demoUser.email, password: 'Demo123!' }).subscribe();
    const request = http.expectOne(`${API}/auth/login`);
    request.flush(authResponse);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: demoUser.email, password: 'Demo123!' });
    expect(service.isAuthenticated()).toBe(true);
    expect(service.token()).toBe(authResponse.token);
    expect(service.currentUser()).toEqual(demoUser);
  });

  it('stays logged out when the login is rejected', () => {
    const service = createService();

    service.login({ email: demoUser.email, password: 'wrong' }).subscribe({ error: () => undefined });
    http.expectOne(`${API}/auth/login`).flush(
      { detail: 'Invalid email or password.' },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(service.isAuthenticated()).toBe(false);
  });

  it('keeps the session after a page reload', () => {
    createService().login({ email: demoUser.email, password: 'Demo123!' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(authResponse);

    TestBed.resetTestingModule();
    const reloaded = createService();

    expect(reloaded.isAuthenticated()).toBe(true);
    expect(reloaded.currentUser()).toEqual(demoUser);
  });

  it('forgets a stored session whose token has already expired', () => {
    // Avoids a request that would fail with 401 (and a flash of the task list) when coming back hours later.
    storeSession(fakeJwt(-60));

    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('taskManager.session')).toBeNull();
  });

  it('keeps a stored session whose token is still valid', () => {
    storeSession(fakeJwt(600));

    expect(createService().isAuthenticated()).toBe(true);
  });

  it('treats a malformed stored token as no session', () => {
    storeSession('not-a-jwt');

    expect(createService().isAuthenticated()).toBe(false);
  });

  it('registers: posts the new user and does not log in automatically', () => {
    const service = createService();
    const newUser = { email: 'new@ballastlane.test', name: 'New', password: 'Secret123' };

    service.register(newUser).subscribe();
    const request = http.expectOne(`${API}/auth/register`);
    request.flush({ id: '2', email: newUser.email, name: newUser.name });

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(newUser);
    expect(service.isAuthenticated()).toBe(false);
  });

  it('logs out: forgets the session and goes to the login page', () => {
    const service = createService();
    service.login({ email: demoUser.email, password: 'Demo123!' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(authResponse);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(localStorage.length).toBe(0);
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
