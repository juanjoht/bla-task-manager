import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthResponse } from '../shared/models/auth.model';
import { AuthService } from './auth.service';

const API = 'http://localhost:5276/api';
const demoUser = { id: '1', email: 'demo@ballastlane.test', name: 'Demo User' };
const authResponse: AuthResponse = { token: 'jwt-token', user: demoUser };

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
    expect(service.token()).toBe('jwt-token');
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
