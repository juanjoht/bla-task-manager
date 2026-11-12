import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { todayIso } from '../../../shared/dates';
import { Task } from '../../../shared/models/task.model';
import { TaskService } from '../task.service';
import { TaskFormPage } from './task-form-page';

function daysFromToday(offset: number): string {
  const [year, month, day] = todayIso().split('-').map(Number);
  const date = new Date(year, month - 1, day + offset);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

const existingTask: Task = {
  id: '42',
  title: 'Prepare demo',
  description: 'Rehearse',
  status: 'InProgress',
  dueDate: daysFromToday(-3), // overdue: must still be editable
  createdAt: '2026-11-01T15:00:00Z',
  updatedAt: '2026-11-01T15:00:00Z',
};

describe('TaskFormPage', () => {
  const taskService = { get: vi.fn(), create: vi.fn(), update: vi.fn() };
  let fixture: ComponentFixture<TaskFormPage>;
  let page: HTMLElement;
  let navigate: ReturnType<typeof vi.spyOn>;

  async function render(taskId: string | null) {
    TestBed.configureTestingModule({
      imports: [TaskFormPage],
      providers: [
        provideRouter([]),
        { provide: TaskService, useValue: taskService },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(taskId ? { id: taskId } : {}) } } },
      ],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(TaskFormPage);
    page = fixture.nativeElement;
    await fixture.whenStable();
  }

  function field<T extends HTMLElement>(selector: string): T {
    return page.querySelector<T>(selector)!;
  }

  function type(selector: string, value: string) {
    const input = field<HTMLInputElement | HTMLTextAreaElement>(selector);
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  async function submit() {
    page.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  beforeEach(() => {
    taskService.get.mockReset();
    taskService.create.mockReset();
    taskService.update.mockReset();
  });

  describe('create mode', () => {
    it('starts empty and has no status field (new tasks are always Pending)', async () => {
      await render(null);

      expect(page.textContent).toContain('New task');
      expect(field('#status')).toBeNull();
    });

    it('validates like the API before saving', async () => {
      await render(null);

      type('#dueDate', daysFromToday(-1));
      await submit();

      expect(taskService.create).not.toHaveBeenCalled();
      expect(page.textContent).toContain('Title is required.');
      expect(page.textContent).toContain('Due date cannot be in the past.');
    });

    it('rejects a title longer than 100 characters', async () => {
      await render(null);

      type('#title', 'a'.repeat(101));
      type('#dueDate', daysFromToday(1));
      await submit();

      expect(taskService.create).not.toHaveBeenCalled();
      expect(page.textContent).toContain('Title cannot exceed 100 characters.');
    });

    it('creates the task and returns to the list', async () => {
      taskService.create.mockReturnValue(of({}));
      await render(null);

      type('#title', '  Write the README  ');
      type('#description', '   ');
      type('#dueDate', daysFromToday(1));
      await submit();

      expect(taskService.create).toHaveBeenCalledWith({
        title: 'Write the README',
        description: null,
        dueDate: daysFromToday(1),
      });
      expect(navigate).toHaveBeenCalledWith(['/tasks']);
    });

    it('shows the API message when saving fails', async () => {
      taskService.create.mockReturnValue(
        throwError(
          () => new HttpErrorResponse({ status: 400, error: { errors: { DueDate: ['Due date cannot be in the past.'] } } }),
        ),
      );
      await render(null);

      type('#title', 'Late night task');
      type('#dueDate', daysFromToday(0));
      await submit();

      expect(page.querySelector('.alert-danger')?.textContent).toContain('Due date cannot be in the past.');
      expect(navigate).not.toHaveBeenCalled();
    });
  });

  describe('edit mode', () => {
    it('loads the task into the form, including its status', async () => {
      taskService.get.mockReturnValue(of(existingTask));

      await render('42');

      expect(taskService.get).toHaveBeenCalledWith('42');
      expect(page.textContent).toContain('Edit task');
      expect(field<HTMLInputElement>('#title').value).toBe('Prepare demo');
      expect(field<HTMLSelectElement>('#status').value).toBe('InProgress');
    });

    it('saves the changes, even when the task is already overdue', async () => {
      taskService.get.mockReturnValue(of(existingTask));
      taskService.update.mockReturnValue(of(existingTask));
      await render('42');

      type('#title', 'Prepare demo v2');
      const status = field<HTMLSelectElement>('#status');
      status.value = 'Done';
      status.dispatchEvent(new Event('change'));
      await submit();

      expect(taskService.update).toHaveBeenCalledWith('42', {
        title: 'Prepare demo v2',
        description: 'Rehearse',
        status: 'Done',
        dueDate: existingTask.dueDate,
      });
      expect(navigate).toHaveBeenCalledWith(['/tasks']);
    });

    it('explains when the task does not exist (or belongs to someone else)', async () => {
      taskService.get.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 404, error: { detail: 'Task not found.' } })),
      );

      await render('999');

      expect(page.querySelector('.alert-danger')?.textContent).toContain('Task not found.');
      expect(page.querySelector('form')).toBeNull();
    });
  });
});
