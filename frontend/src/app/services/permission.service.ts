import {Injectable, inject, computed, signal} from '@angular/core';
import {Observable} from 'rxjs';
import {UserService} from './user.service';
import {PermissionService as ApiPermissionService} from '../api/api/permission.service';
import {PermissionMetadataDto} from '../api';

export interface PermissionMap {
  [key: string]: number;
}

@Injectable({providedIn: 'root'})
export class PermissionService {
  private readonly userService = inject(UserService);
  private readonly apiPermissionService = inject(ApiPermissionService);
  private readonly $permissionsMetadata = signal<PermissionMetadataDto[]>([]);
  private readonly $permissionMap = signal<PermissionMap>({});

  public readonly $permissionsLoaded = computed(() => this.$permissionsMetadata().length > 0);

  constructor() {
  }

  loadPermissions(): Observable<void> {
    return new Observable<void>(observer => {
      this.apiPermissionService.apiPermissionsGet().subscribe({
        next: (response) => {
          if (response.data) {
            this.$permissionsMetadata.set(response.data);

            const map: PermissionMap = {};
            response.data.forEach(p => {
              if (p.key && p.value !== undefined) {
                map[p.key] = p.value;
              }
            });

            this.$permissionMap.set(map);
          }
          observer.next();
          observer.complete();
        },
        error: (error) => {
          console.error('Failed to load permissions metadata:', error);
          observer.next();
          observer.complete();
        }
      });
    });
  }

  getPermission(key: string): number {
    return this.$permissionMap()[key] ?? 0;
  }

  getPermissionsMetadata(): PermissionMetadataDto[] {
    return this.$permissionsMetadata();
  }

  hasPermission(permissionKey: string | number): boolean {
    const user = this.userService.getCurrentUser();
    if (!user || user.permissions === undefined || user.permissions === null) {
      return false;
    }

    if (this.isSuperAdmin()) {
      return true;
    }

    const permissionValue = typeof permissionKey === 'string'
      ? this.getPermission(permissionKey)
      : permissionKey;

    return (user.permissions & permissionValue) === permissionValue;
  }

  hasAnyPermission(...permissionKeys: (string | number)[]): boolean {
    return permissionKeys.some(p => this.hasPermission(p));
  }

  hasAllPermissions(...permissionKeys: (string | number)[]): boolean {
    return permissionKeys.every(p => this.hasPermission(p));
  }

  isSuperAdmin(): boolean {
    const user = this.userService.getCurrentUser();

    if (!user || user.permissions === undefined || user.permissions === null) {
      return false;
    }

    // SuperAdmin bit is 2^31 = 2147483648 (1L << 31 in C#)
    // JavaScript bitwise operators convert to 32-bit SIGNED integers, causing overflow
    // So we check if the value is >= 2147483648 (has bit 31 set)
    // OR check if it's negative when treated as signed (which means bit 31 is set)
    const SUPER_ADMIN_BIT = 2147483648;
    return user.permissions >= SUPER_ADMIN_BIT || (user.permissions | 0) < 0;
  }

  $canReadCatalog = computed(() => this.hasPermission('CatalogRead'));
  $canWriteCatalog = computed(() => this.hasPermission('CatalogWrite'));
  $canDeleteCatalog = computed(() => this.hasPermission('CatalogDelete'));

  $canReadEnrollment = computed(() => this.hasPermission('EnrollmentRead'));
  $canWriteEnrollment = computed(() => this.hasPermission('EnrollmentWrite'));
  $canDeleteEnrollment = computed(() => this.hasPermission('EnrollmentDelete'));

  $canReadAudit = computed(() => this.hasPermission('AuditRead'));

  $canReadUser = computed(() => this.hasPermission('UserRead'));
  $canWriteUser = computed(() => this.hasPermission('UserWrite'));
  $canDeleteUser = computed(() => this.hasPermission('UserDelete'));
  $canManageRoles = computed(() => this.hasPermission('UserManageRoles'));

  $canReadRole = computed(() => this.hasPermission('RoleRead'));
  $canWriteRole = computed(() => this.hasPermission('RoleWrite'));
  $canDeleteRole = computed(() => this.hasPermission('RoleDelete'));

  $canReadAttendance = computed(() => this.hasPermission('AttendanceRead'));
  $canWriteAttendance = computed(() => this.hasPermission('AttendanceWrite'));

  $canAccessAdminDashboard = computed(() =>
    this.isSuperAdmin() || this.hasAnyPermission('UserRead', 'UserWrite', 'AuditRead')
  );

  $canAccessStaffDashboard = computed(() =>
    !this.$canAccessAdminDashboard() &&
    this.hasAnyPermission(
      'AttendanceRead',
      'AttendanceWrite',
      'CatalogRead',
      'CatalogWrite',
      'EnrollmentRead',
      'EnrollmentWrite'
    )
  );
}
