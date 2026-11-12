import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateTaskRequest, Task, TaskStatus, UpdateTaskRequest } from '../../shared/models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  list(_status?: TaskStatus): Observable<Task[]> {
    throw new Error('Not implemented');
  }

  get(_id: string): Observable<Task> {
    throw new Error('Not implemented');
  }

  create(_request: CreateTaskRequest): Observable<Task> {
    throw new Error('Not implemented');
  }

  update(_id: string, _request: UpdateTaskRequest): Observable<Task> {
    throw new Error('Not implemented');
  }

  delete(_id: string): Observable<void> {
    throw new Error('Not implemented');
  }
}
