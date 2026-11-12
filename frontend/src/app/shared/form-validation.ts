import { AbstractControl, ValidatorFn } from '@angular/forms';

/** Same email rule as the API (User.IsValidEmail): something@domain.tld, no spaces, a single @. */
export const EMAIL_FORMAT = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

/** Fails with the given error key when a non-empty value does not match the rule. */
export function matches(rule: RegExp, errorKey: string): ValidatorFn {
  return (control) => (!control.value || rule.test(control.value) ? null : { [errorKey]: true });
}

/** The message for the first failing validator, shown only after the user touched the field or submitted. */
export function firstError(control: AbstractControl, messages: Record<string, string>): string | null {
  if (!control.touched || !control.errors) {
    return null;
  }
  const [firstKey] = Object.keys(control.errors);
  return messages[firstKey] ?? 'Invalid value.';
}
