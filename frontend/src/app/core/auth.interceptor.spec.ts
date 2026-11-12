import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

const API = 'http://localhost:5276/api';

describe('authInterceptor', () => {
  const token = signal<string | null>(null);
  const logout = vi.fn();
  let httpClient: HttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    token.set(null);
    logout.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { token, logout } },
      ],
    });
    httpClient = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('adds the bearer token to API requests when logged in', () => {
    token.set('jwt-token');

    httpClient.get(`${API}/tasks`).subscribe();

    expect(http.expectOne(`${API}/tasks`).request.headers.get('Authorization')).toBe('Bearer jwt-token');
  });

  it('sends no Authorization header when logged out', () => {
    httpClient.get(`${API}/tasks`).subscribe();

    expect(http.expectOne(`${API}/tasks`).request.headers.has('Authorization')).toBe(false);
  });

  it('never sends the token to other hosts', () => {
    token.set('jwt-token');

    httpClient.get('https://other.example/data').subscribe();

    expect(http.expectOne('https://other.example/data').request.headers.has('Authorization')).toBe(false);
  });

  it('logs out when the API answers 401 (expired or invalid token)', () => {
    token.set('expired-token');

    httpClient.get(`${API}/tasks`).subscribe({ error: () => undefined });
    http.expectOne(`${API}/tasks`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logout).toHaveBeenCalled();
  });

  it('does not log out on a failed login attempt (wrong password is also a 401)', () => {
    httpClient.post(`${API}/auth/login`, {}).subscribe({ error: () => undefined });
    http.expectOne(`${API}/auth/login`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logout).not.toHaveBeenCalled();
  });
});
