import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth.service';

/** Frame for the private pages: top bar with the user's name and logout, page content below. */
@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterOutlet],
  template: `
    <nav class="navbar navbar-dark bg-dark">
      <div class="container">
        <a class="navbar-brand" routerLink="/tasks">Task Manager</a>
        <div class="d-flex align-items-center gap-3">
          <span class="navbar-text d-none d-sm-inline">Hi, {{ auth.currentUser()?.name }}</span>
          <button type="button" class="btn btn-outline-light btn-sm" data-testid="logout" (click)="auth.logout()">
            Log out
          </button>
        </div>
      </div>
    </nav>
    <main class="container py-4">
      <router-outlet />
    </main>
  `,
})
export class AppShell {
  protected readonly auth = inject(AuthService);
}
