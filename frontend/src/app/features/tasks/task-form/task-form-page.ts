import { Component, inject, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { apiErrorMessage } from '../../../shared/api-error';
import { todayIso } from '../../../shared/dates';
import { firstError, notBefore, notBlank } from '../../../shared/form-validation';
import { Task, TASK_STATUS_LABELS, TASK_STATUSES, TaskStatus } from '../../../shared/models/task.model';
import { TaskService } from '../task.service';

// Same limits and messages as the API (TaskItem / CreateTaskRequestValidator).
const MESSAGES = {
  title: { required: 'Title is required.', maxlength: 'Title cannot exceed 100 characters.' },
  description: { maxlength: 'Description cannot exceed 500 characters.' },
  dueDate: { required: 'Due date is required.', past: 'Due date cannot be in the past.' },
  status: { required: 'Status is required.' },
};

/** One page for both "new task" (/tasks/new) and "edit task" (/tasks/:id/edit). */
@Component({
  selector: 'app-task-form-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './task-form-page.html',
})
export class TaskFormPage implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly router = inject(Router);

  protected readonly taskId = inject(ActivatedRoute).snapshot.paramMap.get('id');
  protected readonly isEdit = this.taskId !== null;
  protected readonly today = todayIso();
  protected readonly statuses = TASK_STATUSES;
  protected readonly labels = TASK_STATUS_LABELS;

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [notBlank, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
    dueDate: ['', Validators.required],
    status: ['Pending' as TaskStatus, Validators.required],
  });

  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    if (this.isEdit) {
      this.loadTask(this.taskId!);
    } else {
      // Only new tasks must be due today or later; an overdue task stays editable (same rule as the API).
      this.form.controls.dueDate.addValidators(notBefore(this.today));
    }
  }

  protected error(field: keyof typeof MESSAGES): string | null {
    return firstError(this.form.controls[field], MESSAGES[field]);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.save().subscribe({
      next: () => void this.router.navigate(['/tasks']),
      error: (error: unknown) => {
        this.errorMessage.set(apiErrorMessage(error));
        this.saving.set(false);
      },
    });
  }

  private save(): Observable<Task> {
    const { title, description, dueDate, status } = this.form.getRawValue();
    const fields = { title: title.trim(), description: description.trim() || null, dueDate };

    return this.isEdit
      ? this.taskService.update(this.taskId!, { ...fields, status })
      : this.taskService.create(fields);
  }

  private loadTask(id: string): void {
    this.loading.set(true);
    this.taskService.get(id).subscribe({
      next: (task) => {
        this.form.setValue({
          title: task.title,
          description: task.description ?? '',
          dueDate: task.dueDate,
          status: task.status,
        });
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loadError.set(apiErrorMessage(error));
        this.loading.set(false);
      },
    });
  }
}
