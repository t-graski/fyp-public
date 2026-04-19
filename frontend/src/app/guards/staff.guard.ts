import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {PermissionService} from '../services/permission.service';

export const staffGuard: CanActivateFn = () => {
  const permissionService = inject(PermissionService);
  const router = inject(Router);

  // Staff can access if they have catalog read/write OR enrollment read/write permissions
  if (permissionService.hasAnyPermission(
    'CatalogRead',
    'CatalogWrite',
    'EnrollmentRead',
    'EnrollmentWrite',
    'UserRead',
    'SuperAdmin'
  )) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};

