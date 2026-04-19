import {inject} from '@angular/core';
import {Router} from '@angular/router';
import {AppAuthService} from '../api/auth/app-auth.service';

export const authGuard = () => {
  const authService = inject(AppAuthService);
  const router = inject(Router);

  if (authService.getAccessToken()) {
    return true;
  }

  router.navigate(['/']);
  return false;
};

