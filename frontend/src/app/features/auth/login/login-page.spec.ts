import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { NEVER, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  const login = vi.fn();
  let fixture: ComponentFixture<LoginPage>;
  let page: HTMLElement;
  let navigate: ReturnType<typeof vi.spyOn>;

  async function render(queryParams: Record<string, string> = {}) {
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { login } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } } },
      ],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginPage);
    page = fixture.nativeElement;
    await fixture.whenStable();
  }

  function type(selector: string, value: string) {
    const input = page.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  async function submit() {
    page.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  beforeEach(() => login.mockReset());

  it('shows the demo credentials to help reviewers', async () => {
    await render();

    expect(page.textContent).toContain('demo@ballastlane.test');
  });

  it('shows field errors and does not call the API when the form is empty', async () => {
    await render();

    await submit();

    expect(login).not.toHaveBeenCalled();
    expect(page.textContent).toContain('Email is required.');
    expect(page.textContent).toContain('Password is required.');
  });

  it('logs in and goes to the task list', async () => {
    login.mockReturnValue(of({}));
    await render();

    type('#email', 'demo@ballastlane.test');
    type('#password', 'Demo123!');
    await submit();

    expect(login).toHaveBeenCalledWith({ email: 'demo@ballastlane.test', password: 'Demo123!' });
    expect(navigate).toHaveBeenCalledWith(['/tasks']);
  });

  it('shows the API message when the credentials are wrong', async () => {
    login.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 401, error: { detail: 'Invalid email or password.' } })),
    );
    await render();

    type('#email', 'demo@ballastlane.test');
    type('#password', 'Wrong123');
    await submit();

    expect(page.querySelector('.alert-danger')?.textContent).toContain('Invalid email or password.');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('disables the button while the login request is in flight', async () => {
    login.mockReturnValue(NEVER);
    await render();

    type('#email', 'demo@ballastlane.test');
    type('#password', 'Demo123!');
    await submit();

    expect(page.querySelector<HTMLButtonElement>('button[type=submit]')!.disabled).toBe(true);
  });

  it('confirms the account was created when coming from the register page', async () => {
    await render({ registered: 'true' });

    expect(page.querySelector('.alert-success')?.textContent).toContain('Account created');
  });
});
