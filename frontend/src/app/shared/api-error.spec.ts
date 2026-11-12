import { HttpErrorResponse } from '@angular/common/http';
import { apiErrorMessage } from './api-error';

describe('apiErrorMessage', () => {
  it('uses the ProblemDetails "detail" sent by the API', () => {
    const error = new HttpErrorResponse({ status: 409, error: { detail: 'Email is already registered.' } });

    expect(apiErrorMessage(error)).toBe('Email is already registered.');
  });

  it('joins every validation error of a 400 response', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { Title: ['Title is required.'], DueDate: ['Due date cannot be in the past.'] } },
    });

    expect(apiErrorMessage(error)).toBe('Title is required. Due date cannot be in the past.');
  });

  it('explains when the API cannot be reached', () => {
    const error = new HttpErrorResponse({ status: 0 });

    expect(apiErrorMessage(error)).toBe('Cannot reach the server. Is the API running?');
  });

  it('falls back to a generic message for anything else', () => {
    expect(apiErrorMessage(new Error('boom'))).toBe('Something went wrong. Please try again.');
    expect(apiErrorMessage(new HttpErrorResponse({ status: 500, error: {} }))).toBe(
      'Something went wrong. Please try again.',
    );
  });
});
