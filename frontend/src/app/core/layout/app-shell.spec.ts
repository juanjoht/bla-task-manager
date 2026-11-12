import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../auth.service';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  const logout = vi.fn();

  async function render() {
    TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { currentUser: signal({ id: '1', email: 'demo@ballastlane.test', name: 'Demo User' }), logout },
        },
      ],
    });
    const fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('greets the logged-in user', async () => {
    const page = await render();

    expect(page.textContent).toContain('Demo User');
  });

  it('logs out from the header', async () => {
    const page = await render();

    page.querySelector<HTMLButtonElement>('[data-testid=logout]')!.click();

    expect(logout).toHaveBeenCalled();
  });
});
