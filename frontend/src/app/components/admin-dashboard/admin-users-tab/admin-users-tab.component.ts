import {Component, inject, signal, output, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {AdminBulkUserService, AdminCreateUserDto, AdminUserService, UserService} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {AdminUserListItemDto, RoleDto} from '../../../api';
import {DynamicTableComponent, TableColumn, TableAction} from '../../dynamic-table/dynamic-table.component';
import {FormsModule} from '@angular/forms';
import {RoleService} from '../../../services/role.service';
import {HasPermissionDirective} from '../../../directives/has-permission.directive';
import {CsvImportService} from '../../../services/csv-import/csv-import.service';
import {CsvFieldDefinition} from '../../../services/csv-import/csv-import.models';
import {AdminCreateUserDtoBulkItem, BulkCreateUsersRequest} from '../../../api';
import {ConfirmDialogService} from '../../../services/confirm-dialog.service';
import {BulkResultDialogService} from '../../../services/bulk-result-dialog.service';
import {BulkResultItem} from '../../bulk-result-dialog/bulk-result-dialog.models';

@Component({
  selector: 'app-admin-users-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule, DynamicTableComponent, FormsModule, HasPermissionDirective],
  templateUrl: './admin-users-tab.component.html',
  styleUrl: './admin-users-tab.component.scss'
})
export class AdminUsersTabComponent implements OnInit {
  private readonly adminUserService = inject(AdminUserService);
  private readonly adminBulkUserService = inject(AdminBulkUserService);
  private readonly userService = inject(UserService);
  private readonly snackbarService = inject(SnackbarService);
  private readonly csvImportService = inject(CsvImportService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly bulkResultDialogService = inject(BulkResultDialogService);
  protected readonly roleService = inject(RoleService);

  $isLoading = signal(false);
  $users = signal<AdminUserListItemDto[]>([]);
  $showUserDetails = signal<string | null>(null);
  $showCreateUserModal = signal<boolean>(false);
  $showAssignRoleModal = signal<{ userId: string, currentRoles: RoleDto[] } | null>(null);

  $newUser = signal<AdminCreateUserDto>({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    isActive: true,
    roleId: null as string | null
  });

  userColumns: TableColumn<AdminUserListItemDto>[] = [
    {key: 'id', label: 'ID', visible: false, cellClass: 'cell-id'},
    {
      key: 'name',
      label: 'Name',
      visible: true,
      render: (user) => `${user.firstName || ''} ${user.lastName || ''}`.trim() || 'No name'
    },
    {key: 'email', label: 'Email', sortable: true, visible: true},
    {
      key: 'roles',
      label: 'Roles',
      visible: true,
      render: (user) => this.getRoleName(user.roles)
    },
    {
      key: 'isActive',
      label: 'Status',
      sortable: true,
      visible: true,
      render: (user) => user.isActive ? 'Active' : 'Inactive'
    }
  ];

  userActions: TableAction<AdminUserListItemDto>[] = [
    {
      icon: 'visibility',
      label: 'View Details',
      handler: (user) => this.viewUserDetails(user.id!),
      requiredPermission: "UserRead"
    },
    {
      icon: 'school',
      label: 'View Enrollments',
      handler: (user) => this.viewUserEnrollments(user.id!),
      requiredPermission: "EnrollmentRead"
    },
    {
      icon: 'block',
      label: 'Toggle Status',
      handler: (user) => this.toggleUserStatus(user.id!, user.isActive ?? false),
      requiredPermission: "UserWrite"
    },
    {
      divider: true, icon: '', label: '', handler: () => {
      }
    },
    {
      icon: 'admin_panel_settings',
      label: 'Manage Roles',
      handler: (user) => this.openAssignRoleModal(user.id!, user.roles ?? []),
      requiredPermission: "UserManageRoles"
    },
    {
      divider: true, icon: '', label: '', handler: () => {
      }
    },
    {
      icon: 'delete',
      label: 'Delete User',
      danger: true,
      handler: (user) => this.deleteUser(user.id!),
      requiredPermission: "UserDelete",
    }
  ];

  $viewEnrollments = output<string>();

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.$isLoading.set(true);
    this.adminUserService.apiAdminUsersGet().subscribe({
      next: (response) => {
        const userData = response.data;
        if (Array.isArray(userData)) {
          this.$users.set(userData);
        } else {
          this.$users.set([]);
        }
        this.$isLoading.set(false);
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
        this.$isLoading.set(false);
      }
    });
  }

  reloadUsers(): void {
    this.loadUsers();
  }

  getRoleName(roles: RoleDto[] | null | undefined): string {
    if (!roles || roles.length === 0) return 'No roles';
    const roleNames = roles
      .map(role => role.name)
      .filter(name => name != null)
      .join(', ');
    return roleNames || 'Unknown';
  }

  viewUserDetails(userId: string): void {
    this.$showUserDetails.set(userId);
  }

  closeUserDetails(): void {
    this.$showUserDetails.set(null);
  }

  viewUserEnrollments(userId: string): void {
    this.$viewEnrollments.emit(userId);
  }

  toggleUserStatus(userId: string, currentStatus: boolean): void {
    const newStatus = !currentStatus;
    const action = newStatus ? 'activate' : 'deactivate';

    this.confirmDialogService.confirm({
      title: "Confirm Toggle",
      message: `You are about to ${action} this user.`,
      cancelLabel: "Cancel"
    }).subscribe(confirmed => {
      if (!confirmed) return;

      this.updateUserStatus(userId, newStatus);
    })
  }

  updateUserStatus(userId: string, isActive: boolean): void {
    this.adminUserService.apiAdminUsersIdActivePatch(userId, {isActive}).subscribe({
      next: (response: any) => {
        this.snackbarService.showFromApiResponse(response);
        this.loadUsers();
      },
      error: (err: any) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }

  openAssignRoleModal(userId: string, currentRoles: RoleDto[]): void {
    this.$showAssignRoleModal.set({userId, currentRoles});
  }

  closeAssignRoleModal(): void {
    this.$showAssignRoleModal.set(null);
  }

  isRoleAssigned(roleId: string): boolean {
    const modal = this.$showAssignRoleModal();
    if (!modal) return false;
    return modal.currentRoles.some(r => r.id === roleId);
  }

  assignRole(userId: string, roleId: string): void {
    this.userService.apiUsersIdRolesPost(userId, {roleId} as any).subscribe({
      next: (response: any) => {
        this.snackbarService.showFromApiResponse(response);
        this.closeAssignRoleModal();
        this.loadUsers();
      },
      error: (err: any) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }

  deleteUser(userId: string): void {
    this.confirmDialogService.confirm({
      title: "Confirm Deletion",
      message: `You are about to delete this user. This action cannot be undone.`,
      cancelLabel: "Cancel"
    }).subscribe(confirmed => {
      if (!confirmed) return;

      this.adminUserService.apiAdminUsersUserIdDelete(userId).subscribe({
        next: (response) => {
          this.snackbarService.showFromApiResponse(response);
          this.loadUsers();
        },
        error: (err) => {
          this.snackbarService.showFromApiResponse(err.error);
        }
      });
    })
  }

  openCreateUserModal(): void {
    this.$showCreateUserModal.set(true);
  }

  closeCreateUserModal(): void {
    this.$showCreateUserModal.set(false);
  }

  createUser(): void {
    const user = this.$newUser();
    if (!user.firstName || !user.lastName || !user.email || !user.password) {
      this.snackbarService.show('Please fill all fields', 400);
      return;
    }

    this.adminUserService.apiAdminUsersPost({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      password: user.password,
      isActive: user.isActive,
      roleId: user.roleId
    }).subscribe({
      next: (response) => {
        this.snackbarService.showFromApiResponse(response);
        this.closeCreateUserModal();
        this.$newUser.set({
          firstName: '',
          lastName: '',
          email: '',
          password: '',
          isActive: true,
          roleId: null
        });
        this.loadUsers();
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }

  importUsersFromCsv(): void {
    const roleOptions = (this.roleService.$roles() ?? [])
      .filter(r => r.id && r.name)
      .map(r => ({label: r.name!, value: r.id!}));

    const fields: CsvFieldDefinition[] = [
      {key: 'firstName', label: 'First Name', required: true},
      {key: 'lastName', label: 'Last Name', required: true},
      {key: 'email', label: 'Email', required: true},
      {key: 'password', label: 'Password', required: true},
      {key: 'isActive', label: 'Is Active', required: false, defaultValue: 'true'},
      {key: 'roleId', label: 'Role', required: false, options: roleOptions}
    ];

    this.csvImportService.import(fields, {title: 'Import Users from CSV'}).subscribe(result => {
      if (result.data.length === 0) return;

      const items: AdminCreateUserDtoBulkItem[] = result.data.map((row, i) => ({
        key: String(i),
        data: {
          firstName: row['firstName'],
          lastName: row['lastName'],
          email: row['email'],
          password: row['password'],
          isActive: String(row['isActive']).toLowerCase() !== 'false',
          roleId: row['roleId'] || null
        } as AdminCreateUserDto
      }));

      const request: BulkCreateUsersRequest = {
        items,
        continueOnError: true,
        atomic: false
      };

      this.adminBulkUserService.apiAdminBulkUsersCreatePost(request).subscribe({
        next: (response: any) => {
          const rawItems: any[] = response?.data?.items ?? [];

          const resultItems: BulkResultItem[] = items.map((item, i) => {
            const raw = rawItems.find((r: any) => r.key === item.key) ?? rawItems[i];
            return {
              key: item.key ?? String(i),
              input: item.data!,
              success: raw?.success === true,
              errorCode: raw?.errorCode ?? null,
              errorMessage: raw?.message ?? null
            };
          });

          const succeeded = response?.data?.succeeded ?? resultItems.filter(r => r.success).length;
          const failed = response?.data?.failed ?? resultItems.filter(r => !r.success).length;

          if (failed === 0) {
            this.snackbarService.show(`Successfully imported ${succeeded} user${succeeded !== 1 ? 's' : ''}`, 200);
          } else if (succeeded === 0) {
            this.snackbarService.show(`Import failed — all ${failed} users could not be created`, 500);
          } else {
            this.snackbarService.show(`Imported ${succeeded} user${succeeded !== 1 ? 's' : ''}, ${failed} failed`, 207);
          }

          this.bulkResultDialogService.show(resultItems);
          this.loadUsers();
        },
        error: (err: any) => {
          const msg = err?.error?.message || 'Bulk import failed';
          this.snackbarService.show(msg, err?.status || 500);
        }
      });
    });
  }
}
