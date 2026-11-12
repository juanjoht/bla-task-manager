import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { RegisterPage } from './register-page';

describe('RegisterPage', () => {
  const register = vi.fn();
  let fixture: ComponentFixture<RegisterPage>;
  let page: HTMLElement;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    register.mockReset();
    TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [provideRouter([]), { provide: AuthService, useValue: { register } }],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(RegisterPage);
    page = fixture.nativeElement;
    await fixture.whenStable();
  });

  function type(selector: string, value: string) {
    const input = page.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  async function fillAndSubmit(name: string, email: string, password: string) {
    type('#name', name);
    type('#email', email);
    type('#password', password);
    page.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('applies the same password rules as the API before calling it', async () => {
    await fillAndSubmit('Juan', 'juan@test.com', 'juan1234');

    expect(register).not.toHaveBeenCalled();
    expect(page.textContent).toContain('Password must contain an uppercase letter.');
  });

  it('rejects an email without a domain extension, like the API does', async () => {
    await fillAndSubmit('Juan', 'juan@test', 'Secret123');

    expect(register).not.toHaveBeenCalled();
    expect(page.textContent).toContain('Email format is not valid.');
  });

  it('creates the account and goes to the login page', async () => {
    register.mockReturnValue(of({ id: '2', email: 'juan@test.com', name: 'Juan' }));

    await fillAndSubmit('Juan', 'juan@test.com', 'Secret123');

    expect(register).toHaveBeenCalledWith({ name: 'Juan', email: 'juan@test.com', password: 'Secret123' });
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { registered: true } });
  });

  it('shows the API message when the email is already registered', async () => {
    register.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { detail: 'Email is already registered.' } })),
    );

    await fillAndSubmit('Demo', 'demo@ballastlane.test', 'Secret123');

    expect(page.querySelector('.alert-danger')?.textContent).toContain('Email is already registered.');
    expect(navigate).not.toHaveBeenCalled();
  });
});
