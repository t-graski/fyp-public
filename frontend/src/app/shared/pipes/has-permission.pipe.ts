import { Pipe, PipeTransform, inject } from '@angular/core';
import { PermissionService } from '../../services/permission.service';

/**
 * Pipe to check permissions in templates
 *
 * Usage:
 * - Single permission: 'UserWrite' | hasPermission
 * - Multiple permissions (any): ['UserWrite', 'UserDelete'] | hasPermission
 * - All permissions required: ['UserWrite', 'UserDelete'] | hasPermission:'all'
 */
@Pipe({
  name: 'hasPermission',
  standalone: true,
  pure: false
})
export class HasPermissionPipe implements PipeTransform {
  private readonly permissionService = inject(PermissionService);

  transform(permission: string | number | (string | number)[], mode: 'any' | 'all' = 'any'): boolean {
    if (!permission) return true;

    const permissions = Array.isArray(permission) ? permission : [permission];

    if (permissions.length === 0) return true;

    if (mode === 'all') {
      return this.permissionService.hasAllPermissions(...permissions);
    } else {
      return this.permissionService.hasAnyPermission(...permissions);
    }
  }
}
