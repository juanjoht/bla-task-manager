import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth.service';
import { apiErrorMessage } from '../../../shared/api-error';
import { EMAIL_FORMAT, firstError, matches } from '../../../shared/form-validation';

// Same limits and messages as the API validators.
const MESSAGES = {
  name: {
    required: 'Name is required.',
    maxlength: 'Name cannot exceed 100 characters.',
  },
  email: {
    required: 'Email is required.',
    maxlength: 'Email cannot exceed 254 characters.',
    format: 'Email format is not valid.',
  },
  password: {
    required: 'Password is required.',
    minlength: 'Password must be at least 8 characters.',
    uppercase: 'Password must contain an uppercase letter.',
    lowercase: 'Password must contain a lowercase letter.',
    number: 'Password must contain a number.',
  },
};

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register-page.html',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.maxLength(254), matches(EMAIL_FORMAT, 'format')]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        matches(/[A-Z]/, 'uppercase'),
        matches(/[a-z]/, 'lowercase'),
        matches(/[0-9]/, 'number'),
      ],
    ],
  });

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected error(field: keyof typeof MESSAGES): string | null {
    return firstError(this.form.controls[field], MESSAGES[field]);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigate(['/login'], { queryParams: { registered: true } }),
      error: (error: unknown) => {
        this.errorMessage.set(apiErrorMessage(error));
        this.submitting.set(false);
      },
    });
  }
}
