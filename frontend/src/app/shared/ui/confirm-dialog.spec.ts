import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmDialog } from './confirm-dialog';

describe('ConfirmDialog', () => {
  let fixture: ComponentFixture<ConfirmDialog>;
  let page: HTMLElement;
  const confirmed = vi.fn();
  const cancelled = vi.fn();

  beforeEach(async () => {
    confirmed.mockReset();
    cancelled.mockReset();
    TestBed.configureTestingModule({ imports: [ConfirmDialog] });
    fixture = TestBed.createComponent(ConfirmDialog);
    fixture.componentRef.setInput('title', 'Delete task?');
    fixture.componentRef.setInput('message', '"Prepare demo" will be deleted permanently.');
    fixture.componentRef.setInput('confirmLabel', 'Delete');
    fixture.componentInstance.confirmed.subscribe(confirmed);
    fixture.componentInstance.cancelled.subscribe(cancelled);
    page = fixture.nativeElement;
    await fixture.whenStable();
  });

  function click(selector: string) {
    page.querySelector<HTMLElement>(selector)!.click();
  }

  it('is an accessible modal dialog showing the title and message', () => {
    const dialog = page.querySelector('[role=dialog]')!;

    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(dialog.textContent).toContain('Delete task?');
    expect(dialog.textContent).toContain('"Prepare demo" will be deleted permanently.');
  });

  it('uses the given label on the confirm button', () => {
    expect(page.querySelector('[data-testid=confirm]')!.textContent?.trim()).toBe('Delete');
  });

  it('emits "confirmed" from the confirm button', () => {
    click('[data-testid=confirm]');

    expect(confirmed).toHaveBeenCalled();
    expect(cancelled).not.toHaveBeenCalled();
  });

  it('emits "cancelled" from the cancel and close buttons', () => {
    click('[data-testid=cancel]');
    click('[data-testid=close]');

    expect(cancelled).toHaveBeenCalledTimes(2);
    expect(confirmed).not.toHaveBeenCalled();
  });

  it('emits "cancelled" when Escape is pressed', () => {
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(cancelled).toHaveBeenCalled();
  });

  it('emits "cancelled" when clicking outside the dialog box, but not inside it', () => {
    click('.modal-body');
    expect(cancelled).not.toHaveBeenCalled();

    click('.modal');
    expect(cancelled).toHaveBeenCalled();
  });

  it('puts the focus on Cancel, the safe option', async () => {
    await fixture.whenStable();

    expect(document.activeElement).toBe(page.querySelector('[data-testid=cancel]'));
  });
});
