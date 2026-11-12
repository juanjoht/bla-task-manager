import { InjectionToken } from '@angular/core';

/** Base URL of the Task Manager API (see src/TaskManager.Api launch settings). */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  factory: () => 'http://localhost:5276/api',
});
