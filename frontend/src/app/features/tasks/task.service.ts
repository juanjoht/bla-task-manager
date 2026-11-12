import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api.config';
import { CreateTaskRequest, Task, TaskStatus, UpdateTaskRequest } from '../../shared/models/task.model';

/** HTTP client for /api/tasks. The bearer token is added by the auth interceptor. */
@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/tasks`;

  list(status?: TaskStatus): Observable<Task[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<Task[]>(this.url, { params });
  }

  get(id: string): Observable<Task> {
    return this.http.get<Task>(`${this.url}/${id}`);
  }

  create(request: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>(this.url, request);
  }

  update(id: string, request: UpdateTaskRequest): Observable<Task> {
    return this.http.put<Task>(`${this.url}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
