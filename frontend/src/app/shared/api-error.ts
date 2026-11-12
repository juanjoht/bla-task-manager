import { HttpErrorResponse } from '@angular/common/http';

const GENERIC_MESSAGE = 'Something went wrong. Please try again.';

interface ProblemDetails {
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Turns any HTTP error from the API (ProblemDetails / ValidationProblemDetails) into one readable message. */
export function apiErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return GENERIC_MESSAGE;
  }

  if (error.status === 0) {
    return 'Cannot reach the server. Is the API running?';
  }

  const problem = error.error as ProblemDetails | null;
  if (problem?.errors) {
    return Object.values(problem.errors).flat().join(' ');
  }

  return problem?.detail ?? GENERIC_MESSAGE;
}
