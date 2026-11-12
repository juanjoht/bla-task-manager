import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { Task } from '../../../shared/models/task.model';
import { TaskService } from '../task.service';
import { TaskListPage } from './task-list-page';

function localDate(offsetDays: number): string {
  const date = new Date();
  date.setDate(date.getDate() + offsetDays);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

function task(overrides: Partial<Task>): Task {
  return {
    id: '1',
    title: 'Prepare demo',
    description: 'Rehearse',
    status: 'Pending',
    dueDate: localDate(2),
    createdAt: '2026-11-11T15:00:00Z',
    updatedAt: '2026-11-11T15:00:00Z',
    ...overrides,
  };
}

describe('TaskListPage', () => {
  const taskService = { list: vi.fn(), delete: vi.fn() };
  let fixture: ComponentFixture<TaskListPage>;
  let page: HTMLElement;

  async function render() {
    TestBed.configureTestingModule({
      imports: [TaskListPage],
      providers: [provideRouter([]), { provide: TaskService, useValue: taskService }],
    });
    fixture = TestBed.createComponent(TaskListPage);
    page = fixture.nativeElement;
    await fixture.whenStable();
  }

  function click(selector: string) {
    page.querySelector<HTMLElement>(selector)!.click();
  }

  beforeEach(() => {
    taskService.list.mockReset();
    taskService.delete.mockReset();
  });

  it('shows the user tasks with a readable status', async () => {
    taskService.list.mockReturnValue(
      of([task({ id: '1', title: 'Prepare demo' }), task({ id: '2', title: 'Review PRs', status: 'InProgress' })]),
    );

    await render();

    expect(page.textContent).toContain('Prepare demo');
    expect(page.textContent).toContain('Review PRs');
    expect(page.textContent).toContain('In progress');
  });

  it('reloads the list when the status filter changes', async () => {
    taskService.list.mockReturnValue(of([]));
    await render();

    const filter = page.querySelector<HTMLSelectElement>('#statusFilter')!;
    filter.value = 'Done';
    filter.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    expect(taskService.list).toHaveBeenLastCalledWith('Done');
  });

  it('shows a friendly message when there are no tasks', async () => {
    taskService.list.mockReturnValue(of([]));

    await render();

    expect(page.textContent).toContain('No tasks yet');
  });

  it('flags tasks that are past their due date and not done', async () => {
    taskService.list.mockReturnValue(
      of([
        task({ id: '1', title: 'Late', dueDate: localDate(-1), status: 'Pending' }),
        task({ id: '2', title: 'Late but done', dueDate: localDate(-1), status: 'Done' }),
      ]),
    );

    await render();

    const rows = page.querySelectorAll('[data-testid=task]');
    expect(rows[0].textContent).toContain('Overdue');
    expect(rows[1].textContent).not.toContain('Overdue');
  });

  it('asks for confirmation in a dialog that names the task', async () => {
    taskService.list.mockReturnValue(of([task({ id: '1', title: 'Prepare demo' })]));
    await render();

    click('[data-testid=delete]');
    await fixture.whenStable();

    const dialog = page.querySelector('[role=dialog]');
    expect(taskService.delete).not.toHaveBeenCalled();
    expect(dialog?.textContent).toContain('Delete task?');
    expect(dialog?.textContent).toContain('Prepare demo');
  });

  it('deletes after confirming and removes the task from the list', async () => {
    taskService.list.mockReturnValue(of([task({ id: '1', title: 'To delete' })]));
    taskService.delete.mockReturnValue(of(undefined));
    await render();

    click('[data-testid=delete]');
    await fixture.whenStable();
    click('[role=dialog] [data-testid=confirm]');
    await fixture.whenStable();

    expect(taskService.delete).toHaveBeenCalledWith('1');
    expect(page.textContent).not.toContain('To delete');
    expect(page.querySelector('[role=dialog]')).toBeNull();
  });

  it('keeps the task when the deletion is cancelled', async () => {
    taskService.list.mockReturnValue(of([task({ id: '1', title: 'Keep me' })]));
    await render();

    click('[data-testid=delete]');
    await fixture.whenStable();
    click('[role=dialog] [data-testid=cancel]');
    await fixture.whenStable();

    expect(taskService.delete).not.toHaveBeenCalled();
    expect(page.textContent).toContain('Keep me');
    expect(page.querySelector('[role=dialog]')).toBeNull();
  });

  it('shows the error when the tasks cannot be loaded', async () => {
    taskService.list.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));

    await render();

    expect(page.querySelector('.alert-danger')?.textContent).toContain('Cannot reach the server');
  });
});
