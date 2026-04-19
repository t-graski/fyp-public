import {Component, inject, signal, OnInit, computed} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {MatIconModule} from '@angular/material/icon';
import {AdminUserService} from '../../../api';
import {AdminUserDetailDto, AdminUpsertStudentRecordDto, AdminUserListItemDto} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {DynamicTableComponent, TableColumn} from '../../dynamic-table/dynamic-table.component';
import {HasPermissionDirective} from '../../../directives/has-permission.directive';

@Component({
  selector: 'app-admin-student-records-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, DynamicTableComponent, HasPermissionDirective],
  templateUrl: './admin-student-records-tab.component.html',
  styleUrl: './admin-student-records-tab.component.scss'
})
export class AdminStudentRecordsTabComponent implements OnInit {
  private readonly adminUserService = inject(AdminUserService);
  private readonly snackbarService = inject(SnackbarService);

  $isLoading = signal(false);
  $allUsers = signal<AdminUserListItemDto[]>([]);
  $students = computed(() => this.$allUsers().filter(u => !!u.student));

  $selectedUserId = signal<string | null>(null);
  $userDetail = signal<AdminUserDetailDto | null>(null);
  $detailLoading = signal(false);

  $editing = signal(false);
  $saving = signal(false);
  $form = signal<AdminUpsertStudentRecordDto>({
    personalEmail: '',
    homeAddress: '',
    phoneNumber: '',
    entryQualifications: [],
    gender: ''
  });
  $qualificationInput = signal('');

  userColumns: TableColumn<AdminUserListItemDto>[] = [
    {key: 'id', label: 'ID', visible: false},
    {
      key: 'name',
      label: 'Name',
      visible: true,
      render: (u) => `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim() || 'No name'
    },
    {key: 'email', label: 'Email', sortable: true, visible: true},
    {
      key: 'student',
      label: 'Student Number',
      sortable: true,
      visible: true,
      render: (u) => u.student?.studentNumber ?? '-',
      value: (u) => u.student?.studentNumber
    },
    {
      key: 'isActive',
      label: 'Status',
      sortable: true,
      visible: true,
      render: (u) => u.isActive ? 'Active' : 'Inactive'
    }
  ];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.$isLoading.set(true);
    this.adminUserService.apiAdminUsersGet().subscribe({
      next: (res) => {
        this.$allUsers.set(Array.isArray(res.data) ? res.data : []);
        this.$isLoading.set(false);
      },
      error: (err) => {
        this.snackbarService.show(err?.error?.error?.message || 'Failed to load users', 500);
        this.$isLoading.set(false);
      }
    });
  }

  openRecord(userId: string): void {
    this.$selectedUserId.set(userId);
    this.$userDetail.set(null);
    this.$editing.set(false);
    this.$detailLoading.set(true);
    this.adminUserService.apiAdminUsersUserIdGet(userId).subscribe({
      next: (res) => {
        this.$userDetail.set(res.data ?? null);
        this.$detailLoading.set(false);
      },
      error: (err) => {
        this.snackbarService.show(err?.error?.error?.message || 'Failed to load user details', 500);
        this.$detailLoading.set(false);
      }
    });
  }

  closeRecord(): void {
    this.$selectedUserId.set(null);
    this.$userDetail.set(null);
    this.$editing.set(false);
  }

  startEdit(): void {
    const rec = this.$userDetail()?.studentRecord;
    this.$form.set({
      personalEmail: rec?.personalEmail ?? '',
      homeAddress: rec?.homeAddress ?? '',
      phoneNumber: rec?.phoneNumber ?? '',
      entryQualifications: rec?.entryQualifications ? [...rec.entryQualifications] : [],
      gender: rec?.gender ?? ''
    });
    this.$qualificationInput.set('');
    this.$editing.set(true);
  }

  cancelEdit(): void {
    this.$editing.set(false);
  }

  addQualification(): void {
    const val = this.$qualificationInput().trim();
    if (!val) return;
    this.$form.update(f => ({...f, entryQualifications: [...(f.entryQualifications ?? []), val]}));
    this.$qualificationInput.set('');
  }

  removeQualification(i: number): void {
    this.$form.update(f => {
      const q = [...(f.entryQualifications ?? [])];
      q.splice(i, 1);
      return {...f, entryQualifications: q};
    });
  }

  setFormField(field: keyof AdminUpsertStudentRecordDto, value: string): void {
    this.$form.update(f => ({...f, [field]: value}));
  }

  saveRecord(): void {
    const userId = this.$selectedUserId();
    if (!userId) return;
    this.$saving.set(true);
    this.adminUserService.apiAdminUsersUsersUserIdStudentRecordPut(userId, this.$form()).subscribe({
      next: (res: any) => {
        this.snackbarService.showFromApiResponse(res);
        this.$saving.set(false);
        this.$editing.set(false);
        this.reloadDetail(userId);
      },
      error: (err: any) => {
        this.snackbarService.show(err?.error?.error?.message || 'Failed to save student record', 500);
        this.$saving.set(false);
      }
    });
  }

  deleteRecord(): void {
    const userId = this.$selectedUserId();
    if (!userId) return;
    if (!confirm('Are you sure you want to delete this student record?')) return;
    this.adminUserService.apiAdminUsersUsersUserIdStudentRecordDelete(userId).subscribe({
      next: () => {
        this.snackbarService.show('Student record deleted', 200);
        this.reloadDetail(userId);
      },
      error: (err: any) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }

  private reloadDetail(userId: string): void {
    this.adminUserService.apiAdminUsersUserIdGet(userId).subscribe({
      next: (res) => this.$userDetail.set(res.data ?? null)
    });
  }
}
