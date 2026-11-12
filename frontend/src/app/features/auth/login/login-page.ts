import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth.service';
import { apiErrorMessage } from '../../../shared/api-error';
import { firstError } from '../../../shared/form-validation';

const MESSAGES = {
  email: { required: 'Email is required.' },
  password: { required: 'Password is required.' },
};

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly justRegistered =
    inject(ActivatedRoute).snapshot.queryParamMap.get('registered') === 'true';

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', Validators.required],
    password: ['', Validators.required],
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
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigate(['/tasks']),
      error: (error: unknown) => {
        this.errorMessage.set(apiErrorMessage(error));
        this.submitting.set(false);
      },
    });
  }
}
