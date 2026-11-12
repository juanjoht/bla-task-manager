import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  template: '',
})
export class ConfirmDialog {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input('Confirm');
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
