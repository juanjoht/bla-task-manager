import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register-page').then((m) => m.RegisterPage),
  },
  {
    // Private area: the guard sends logged-out users to /login.
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./core/layout/app-shell').then((m) => m.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'tasks' },
      {
        path: 'tasks',
        loadComponent: () => import('./features/tasks/task-list/task-list-page').then((m) => m.TaskListPage),
      },
      {
        path: 'tasks/new',
        loadComponent: () => import('./features/tasks/task-form/task-form-page').then((m) => m.TaskFormPage),
      },
      {
        path: 'tasks/:id/edit',
        loadComponent: () => import('./features/tasks/task-form/task-form-page').then((m) => m.TaskFormPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
