import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessage } from '../../../shared/api-error';
import { formatDueDate, todayIso } from '../../../shared/dates';
import { Task, TASK_STATUS_LABELS, TASK_STATUSES, TaskStatus } from '../../../shared/models/task.model';
import { TaskService } from '../task.service';

const STATUS_BADGES: Record<TaskStatus, string> = {
  Pending: 'text-bg-secondary',
  InProgress: 'text-bg-primary',
  Done: 'text-bg-success',
};

@Component({
  selector: 'app-task-list-page',
  imports: [RouterLink],
  templateUrl: './task-list-page.html',
})
export class TaskListPage implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly today = todayIso();

  protected readonly statuses = TASK_STATUSES;
  protected readonly labels = TASK_STATUS_LABELS;
  protected readonly badges = STATUS_BADGES;
  protected readonly formatDueDate = formatDueDate;

  protected readonly tasks = signal<Task[]>([]);
  protected readonly statusFilter = signal<TaskStatus | ''>('');
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pendingDeleteId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected changeFilter(value: string): void {
    this.statusFilter.set(value as TaskStatus | '');
    this.load();
  }

  protected isOverdue(task: Task): boolean {
    return task.status !== 'Done' && task.dueDate < this.today;
  }

  protected askToDelete(id: string): void {
    this.pendingDeleteId.set(id);
  }

  protected cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  protected confirmDelete(id: string): void {
    this.taskService.delete(id).subscribe({
      next: () => {
        this.tasks.update((tasks) => tasks.filter((task) => task.id !== id));
        this.pendingDeleteId.set(null);
      },
      error: (error: unknown) => this.errorMessage.set(apiErrorMessage(error)),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.taskService.list(this.statusFilter() || undefined).subscribe({
      next: (tasks) => {
        this.tasks.set(tasks);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(apiErrorMessage(error));
        this.loading.set(false);
      },
    });
  }
}
