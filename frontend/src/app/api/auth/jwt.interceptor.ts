import {HttpInterceptorFn} from '@angular/common/http';
import {inject} from '@angular/core';
import {AppAuthService} from './app-auth.service';

const AUTH_FREE_PATHS = ['/api/auth/login', 'api/auth/register'];

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  if (AUTH_FREE_PATHS.some(p => req.url.includes(p))) {
    const stripped = req.clone({headers: req.headers.delete('Authorization')});
    return next(stripped);
  }

  const auth = inject(AppAuthService);
  const token = auth.getAccessToken();

  if (!token) {
    return next(req);
  }

  const authedReq = req.clone({setHeaders: {Authorization: `Bearer ${token}`}})

  return next(authedReq);
}
