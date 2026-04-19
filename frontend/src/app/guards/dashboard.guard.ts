import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {PermissionService} from '../services/permission.service';

export const dashboardGuard: CanActivateFn = () => {
  const permissionService = inject(PermissionService);
  const router = inject(Router);

  if (permissionService.$canAccessAdminDashboard()) {
    return router.createUrlTree(['/admin']);
  }

  if (permissionService.$canAccessStaffDashboard()) {
    return router.createUrlTree(['/staff']);
  }

  return true;
};
