import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule, RouterLink],
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected email = '';
  protected password = '';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  async submit(): Promise<void> {
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.login(this.email, this.password);
      this.router.navigateByUrl('/dashboard');
    } catch (err) {
      this.error.set(
        err instanceof HttpErrorResponse && err.status === 401
          ? 'Incorrect email or password.'
          : 'Something went wrong. Please try again.',
      );
    } finally {
      this.submitting.set(false);
    }
  }

  async viewDemo(): Promise<void> {
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.loginAsDemo();
      this.router.navigateByUrl('/dashboard');
    } catch {
      this.error.set('Could not start the demo. Please try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
