import { AbstractControl, ValidatorFn } from '@angular/forms';

/** Same email rule as the API (User.IsValidEmail): something@domain.tld, no spaces, a single @. */
export const EMAIL_FORMAT = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

/** Fails with the given error key when a non-empty value does not match the rule. */
export function matches(rule: RegExp, errorKey: string): ValidatorFn {
  return (control) => (!control.value || rule.test(control.value) ? null : { [errorKey]: true });
}

/** Like Validators.required, but a value made only of spaces also counts as empty (same as the API). */
export const notBlank: ValidatorFn = (control) =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };

/** Fails with "past" when an ISO date ("yyyy-MM-dd") is before the given minimum. */
export function notBefore(minimumIsoDate: string): ValidatorFn {
  return (control) => (!control.value || control.value >= minimumIsoDate ? null : { past: true });
}

/** The message for the first failing validator, shown only after the user touched the field or submitted. */
export function firstError(control: AbstractControl, messages: Record<string, string>): string | null {
  if (!control.touched || !control.errors) {
    return null;
  }
  const [firstKey] = Object.keys(control.errors);
  return messages[firstKey] ?? 'Invalid value.';
}
