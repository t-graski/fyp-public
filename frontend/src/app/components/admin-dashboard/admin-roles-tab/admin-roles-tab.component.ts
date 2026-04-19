import {Component, inject, OnInit, signal} from '@angular/core';
import {RoleDto, RoleService} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {PermissionService} from '../../../services/permission.service';
import {DynamicTableComponent, TableColumn, TableAction} from '../../dynamic-table/dynamic-table.component';
import {FormsModule} from '@angular/forms';
import {HasPermissionDirective} from '../../../directives/has-permission.directive';
import {MatIcon} from '@angular/material/icon';
import {CommonModule} from '@angular/common';

interface PermissionCheckbox {
  key: string;
  description: string;
  value: number;
  checked: boolean;
}

@Component({
  selector: 'app-admin-roles-tab',
  imports: [
    CommonModule,
    DynamicTableComponent,
    FormsModule,
    HasPermissionDirective,
    MatIcon
  ],
  templateUrl: './admin-roles-tab.component.html',
  styleUrl: './admin-roles-tab.component.scss',
  standalone: true
})
export class AdminRolesTab implements OnInit {
  private readonly rolesService = inject(RoleService);
  private readonly permissionService = inject(PermissionService);
  private readonly snackbarService = inject(SnackbarService);

  $isLoading = signal(false);
  $roles = signal<RoleDto[]>([]);
  $showCreateRoleModal = signal(false);
  $showEditRoleModal = signal<string | null>(null);
  $showRoleDetails = signal<string | null>(null);

  $newRole = signal({
    name: '',
    permissions: [] as PermissionCheckbox[]
  });

  $editRole = signal({
    id: '',
    name: '',
    permissions: [] as PermissionCheckbox[]
  });

  roleColumns: TableColumn<RoleDto>[] = [
    {key: 'id', label: 'ID', visible: false, cellClass: 'cell-id'},
    {key: 'name', label: 'Name', sortable: true, visible: true},
    {
      key: 'permissions',
      label: 'Permission Value',
      sortable: true,
      visible: true,
      render: (role) => (role.permissions || 0).toString()
    },
    {key: 'rank', label: 'Rank', visible: true, sortable: true},
    {key: 'key', label: 'Key', visible: false},
    {
      key: 'isSystem',
      label: 'System Role',
      visible: false,
      sortable: true,
      render: (role) => role.isSystem ? 'Yes' : 'No'
    }
  ];

  roleActions: TableAction<RoleDto>[] = [
    {
      icon: 'visibility',
      label: 'View Details',
      handler: (role) => this.viewRoleDetails(role.id!),
      requiredPermission: 'RoleRead'
    },
    {
      icon: 'edit',
      label: 'Edit Role',
      handler: (role) => this.openEditRoleModal(role),
      requiredPermission: 'RoleWrite',
      disabled: (role) => role.isSystem === true
    },
    {
      divider: true, icon: '', label: '', handler: () => {
      }
    },
    {
      icon: 'delete',
      label: 'Delete Role',
      danger: true,
      handler: (role) => this.deleteRole(role.id!),
      requiredPermission: 'RoleDelete',
      disabled: (role) => role.isSystem === true
    }
  ];

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.$isLoading.set(true);
    this.rolesService.apiRolesGet().subscribe({
      next: (response) => {
        const rolesData = response.data;
        if (Array.isArray(rolesData)) {
          this.$roles.set(rolesData);
        } else {
          this.$roles.set([]);
        }
        this.$isLoading.set(false);
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
        this.$roles.set([]);
        this.$isLoading.set(false);
      }
    });
  }

  reloadRoles(): void {
    this.loadRoles();
  }

  getAvailablePermissions(): PermissionCheckbox[] {
    const metadata = this.permissionService.getPermissionsMetadata();
    return metadata
      .filter(p => p.key !== 'SuperAdmin') // Don't allow assigning SuperAdmin
      .map(p => ({
        key: p.key || '',
        description: p.description || '',
        value: p.value || 0,
        checked: false
      }));
  }

  openCreateRoleModal(): void {
    this.$newRole.set({
      name: '',
      permissions: this.getAvailablePermissions()
    });
    this.$showCreateRoleModal.set(true);
  }

  closeCreateRoleModal(): void {
    this.$showCreateRoleModal.set(false);
  }

  createRole(): void {
    const role = this.$newRole();
    if (!role.name.trim()) {
      this.snackbarService.show('Role name is required', 400);
      return;
    }

    const permissionValue = role.permissions
      .filter(p => p.checked)
      .reduce((sum, p) => sum + p.value, 0);

    this.rolesService.apiRolesPost({
      name: role.name,
      permissions: permissionValue
    }).subscribe({
      next: (response) => {
        this.snackbarService.showFromApiResponse(response);
        this.closeCreateRoleModal();
        this.loadRoles();
      },
      error: (err) => {
        const errorCode = err?.error?.error?.code || err?.error?.code || 'ERROR';
        const errorMessage = err?.error?.error?.message || err?.error?.message || 'Failed to create role';
        this.snackbarService.show(`${errorCode}: ${errorMessage}`, err?.status || 500);
      }
    });
  }

  openEditRoleModal(role: RoleDto): void {
    const permissions = this.getAvailablePermissions();

    permissions.forEach(p => {
      p.checked = ((role.permissions || 0) & p.value) === p.value;
    });

    this.$editRole.set({
      id: role.id || '',
      name: role.name || '',
      permissions: permissions
    });
    this.$showEditRoleModal.set(role.id || null);
  }

  closeEditRoleModal(): void {
    this.$showEditRoleModal.set(null);
  }

  updateRole(): void {
    const role = this.$editRole();
    if (!role.name.trim()) {
      this.snackbarService.show('Role name is required', 400);
      return;
    }

    const permissionValue = role.permissions
      .filter(p => p.checked)
      .reduce((sum, p) => sum + p.value, 0);

    this.rolesService.apiRolesIdPut(role.id, {
      name: role.name,
      permissions: permissionValue
    }).subscribe({
      next: (response) => {
        this.snackbarService.showFromApiResponse(response);
        this.closeEditRoleModal();
        this.loadRoles();
      },
      error: (err) => {
        const errorCode = err?.error?.error?.code || err?.error?.code || 'ERROR';
        const errorMessage = err?.error?.error?.message || err?.error?.message || 'Failed to update role';
        this.snackbarService.show(`${errorCode}: ${errorMessage}`, err?.status || 500);
      }
    });
  }

  deleteRole(roleId: string): void {
    if (!confirm('Are you sure you want to delete this role? Users with this role will lose these permissions.')) {
      return;
    }

    this.rolesService.apiRolesIdDelete(roleId).subscribe({
      next: (response) => {
        this.snackbarService.showFromApiResponse(response);
        this.loadRoles();
      },
      error: (err) => {
        const errorCode = err?.error?.error?.code || err?.error?.code || 'ERROR';
        const errorMessage = err?.error?.error?.message || err?.error?.message || 'Failed to delete role';
        this.snackbarService.show(`${errorCode}: ${errorMessage}`, err?.status || 500);
      }
    });
  }

  viewRoleDetails(roleId: string): void {
    this.$showRoleDetails.set(roleId);
  }

  closeRoleDetails(): void {
    this.$showRoleDetails.set(null);
  }

  getPermissionDetails(permissionValue: number): Array<{ key: string, bit: number }> {
    const metadata = this.permissionService.getPermissionsMetadata();

    return metadata
      .filter(p => {
        const pValue = p.value || 0;
        if (pValue === 0) return false;

        // Special handling for SuperAdmin (2^31 = 2147483648)
        // JavaScript bitwise operators don't work correctly with bit 31 due to signed integer conversion
        if (pValue === 2147483648) {
          return permissionValue >= 2147483648 || (permissionValue | 0) < 0;
        }

        return (permissionValue & pValue) === pValue;
      })
      .map(p => ({
        key: p.key || 'Unknown',
        bit: p.bit || 0
      }));
  }

  toggleAllPermissions(checked: boolean): void {
    const role = this.$newRole();
    role.permissions.forEach(p => p.checked = checked);
    this.$newRole.set({...role});
  }

  toggleAllPermissionsEdit(checked: boolean): void {
    const role = this.$editRole();
    role.permissions.forEach(p => p.checked = checked);
    this.$editRole.set({...role});
  }
}
