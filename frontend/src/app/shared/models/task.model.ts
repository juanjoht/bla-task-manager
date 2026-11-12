/** Same values the API serializes as text. */
export type TaskStatus = 'Pending' | 'InProgress' | 'Done';

export const TASK_STATUSES: readonly TaskStatus[] = ['Pending', 'InProgress', 'Done'];

export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  Pending: 'Pending',
  InProgress: 'In progress',
  Done: 'Done',
};

/** Mirrors the API's TaskDto. Dates are ISO strings: dueDate "yyyy-MM-dd", audit fields in UTC. */
export interface Task {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  dueDate: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  dueDate: string;
}

export interface UpdateTaskRequest extends CreateTaskRequest {
  status: TaskStatus;
}
