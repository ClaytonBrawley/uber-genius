import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.getToken();

  const authedReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authedReq).pipe(
    catchError((err) => {
      // Exclude /api/auth/* — a wrong password on the login form itself returns 401 and
      // should show an inline error, not log the user out of a session they never had.
      if (err.status === 401 && !req.url.includes('/api/auth/')) {
        auth.logout();
        router.navigateByUrl('/login');
      }
      return throwError(() => err);
    }),
  );
};
