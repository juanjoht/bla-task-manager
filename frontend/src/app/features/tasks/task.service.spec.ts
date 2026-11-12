import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TaskService } from './task.service';

const API = 'http://localhost:5276/api';

describe('TaskService', () => {
  let service: TaskService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(TaskService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists all tasks when no status is given', () => {
    service.list().subscribe();

    const request = http.expectOne(`${API}/tasks`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.has('status')).toBe(false);
  });

  it('sends the status filter as a query parameter', () => {
    service.list('Done').subscribe();

    expect(http.expectOne(`${API}/tasks?status=Done`).request.method).toBe('GET');
  });

  it('gets one task by id', () => {
    service.get('42').subscribe();

    expect(http.expectOne(`${API}/tasks/42`).request.method).toBe('GET');
  });

  it('creates a task', () => {
    const body = { title: 'New', description: null, dueDate: '2026-11-20' };

    service.create(body).subscribe();

    const request = http.expectOne(`${API}/tasks`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
  });

  it('updates a task', () => {
    const body = { title: 'Edited', description: 'x', status: 'Done' as const, dueDate: '2026-11-20' };

    service.update('42', body).subscribe();

    const request = http.expectOne(`${API}/tasks/42`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
  });

  it('deletes a task', () => {
    service.delete('42').subscribe();

    expect(http.expectOne(`${API}/tasks/42`).request.method).toBe('DELETE');
  });
});
