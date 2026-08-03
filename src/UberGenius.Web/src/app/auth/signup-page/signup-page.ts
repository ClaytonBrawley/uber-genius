import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-signup-page',
  imports: [FormsModule, RouterLink],
  templateUrl: './signup-page.html',
})
export class SignupPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected email = '';
  protected password = '';
  protected displayName = '';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  async submit(): Promise<void> {
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.signup(this.email, this.password, this.displayName);
      this.router.navigateByUrl('/dashboard');
    } catch (err) {
      this.error.set(
        err instanceof HttpErrorResponse && err.status === 409
          ? 'An account with that email already exists.'
          : 'Something went wrong. Please try again.',
      );
    } finally {
      this.submitting.set(false);
    }
  }
}
