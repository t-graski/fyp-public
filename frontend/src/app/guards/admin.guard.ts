import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {PermissionService} from '../services/permission.service';
import {UserService} from '../services/user.service';
import {map, take, filter} from 'rxjs';
import {toObservable} from '@angular/core/rxjs-interop';

export const adminGuard: CanActivateFn = () => {
  const permissionService = inject(PermissionService);
  const userService = inject(UserService);
  const router = inject(Router);

  const permissionsLoaded$ = toObservable(permissionService.$permissionsLoaded);

  return permissionsLoaded$.pipe(
    filter(loaded => loaded),
    take(1),
    map(() => {
      if (permissionService.isSuperAdmin()) {
        return true;
      }

      const user = userService.getCurrentUser();
      const hasAdminRole = user?.roles?.some(role => role.key?.toLowerCase() === 'admin') || false;

      if (hasAdminRole) {
        return true;
      }

      if (permissionService.hasAnyPermission(
        'UserRead',
        'UserWrite',
        'CatalogRead',
        'CatalogWrite',
        'EnrollmentRead',
        'EnrollmentWrite',
        'AuditRead',
        'RoleRead'
      )) {
        return true;
      }

      return router.createUrlTree(['/dashboard']);
    })
  );
};

