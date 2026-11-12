import { DOCUMENT } from '@angular/common';
import { afterNextRender, Component, DestroyRef, ElementRef, inject, input, output, viewChild } from '@angular/core';

let nextId = 0;

/**
 * Reusable confirmation modal. Uses Bootstrap's modal markup and styles, but Angular controls it
 * (shown with @if by the parent), so Bootstrap's JavaScript is not needed.
 */
@Component({
  selector: 'app-confirm-dialog',
  host: { '(document:keydown.escape)': 'cancelled.emit()' },
  template: `
    <div class="modal-backdrop fade show"></div>
    <div class="modal fade show d-block" tabindex="-1" role="dialog" aria-modal="true"
         [attr.aria-labelledby]="titleId" (click)="closeOnOutsideClick($event)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h2 class="modal-title fs-5" [id]="titleId">{{ title() }}</h2>
            <button type="button" class="btn-close" aria-label="Close" data-testid="close" (click)="cancelled.emit()"></button>
          </div>
          <div class="modal-body">{{ message() }}</div>
          <div class="modal-footer">
            <button #cancelButton type="button" class="btn btn-outline-secondary" data-testid="cancel" (click)="cancelled.emit()">
              Cancel
            </button>
            <button type="button" class="btn btn-danger" data-testid="confirm" (click)="confirmed.emit()">
              {{ confirmLabel() }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class ConfirmDialog {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input('Confirm');
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  protected readonly titleId = `confirm-dialog-title-${nextId++}`;
  private readonly cancelButton = viewChild.required<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    // Start on the safe option, and stop the page behind the dialog from scrolling while it is open.
    afterNextRender(() => this.cancelButton().nativeElement.focus());

    const body = inject(DOCUMENT).body;
    body.classList.add('modal-open');
    inject(DestroyRef).onDestroy(() => body.classList.remove('modal-open'));
  }

  /** Clicks on the dark area around the box close the dialog; clicks inside the box do not. */
  protected closeOnOutsideClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.cancelled.emit();
    }
  }
}
