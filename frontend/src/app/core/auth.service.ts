import { Injectable, Signal } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../shared/models/auth.model';
import { User } from '../shared/models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly token!: Signal<string | null>;
  readonly currentUser!: Signal<User | null>;
  readonly isAuthenticated!: Signal<boolean>;

  login(_request: LoginRequest): Observable<AuthResponse> {
    throw new Error('Not implemented');
  }

  register(_request: RegisterRequest): Observable<User> {
    throw new Error('Not implemented');
  }

  logout(): void {
    throw new Error('Not implemented');
  }
}
