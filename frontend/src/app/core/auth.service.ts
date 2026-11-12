import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../shared/models/auth.model';
import { User } from '../shared/models/user.model';
import { API_BASE_URL } from './api.config';

const SESSION_KEY = 'taskManager.session';

/** Holds the logged-in session (token + user) in a signal, persisted in localStorage so it survives reloads. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = inject(API_BASE_URL);

  private readonly session = signal<AuthResponse | null>(readStoredSession());

  readonly token = computed(() => this.session()?.token ?? null);
  readonly currentUser = computed(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/auth/login`, request)
      .pipe(tap((response) => this.startSession(response)));
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/auth/register`, request);
  }

  logout(): void {
    localStorage.removeItem(SESSION_KEY);
    this.session.set(null);
    void this.router.navigate(['/login']);
  }

  private startSession(response: AuthResponse): void {
    localStorage.setItem(SESSION_KEY, JSON.stringify(response));
    this.session.set(response);
  }
}

function readStoredSession(): AuthResponse | null {
  try {
    const stored = localStorage.getItem(SESSION_KEY);
    const session = stored ? (JSON.parse(stored) as AuthResponse) : null;

    if (session && isExpired(session.token)) {
      localStorage.removeItem(SESSION_KEY);
      return null;
    }
    return session;
  } catch {
    return null; // corrupted value: start logged out
  }
}

/**
 * Reads the "exp" claim of a JWT. This is only a UX shortcut (skip a request that would fail with 401):
 * the browser cannot verify the signature, so the API still validates every token.
 */
function isExpired(token: string): boolean {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const { exp } = JSON.parse(atob(payload)) as { exp?: unknown };
    return typeof exp !== 'number' || exp * 1000 <= Date.now();
  } catch {
    return true; // not a readable JWT
  }
}
