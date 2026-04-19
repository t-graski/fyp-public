import {Component, inject, signal, input, computed} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {FormsModule} from '@angular/forms';
import {AdminEnrollmentService} from '../../../api';
import {EnrollmentQueriesService} from '../../../api';
import {AdminCatalogService} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {AdminUserListItemDto} from '../../../api';
import {AdminCourseDto} from '../../../api';
import {AdminModuleDto} from '../../../api';

@Component({
  selector: 'app-admin-enroll-module-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule, FormsModule],
  templateUrl: './admin-enroll-module-tab.component.html',
  styleUrl: './admin-enroll-module-tab.component.scss'
})
export class AdminEnrollModuleTabComponent {
  private readonly adminEnrollmentService = inject(AdminEnrollmentService);
  private readonly enrollmentQueriesService = inject(EnrollmentQueriesService);
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly snackbarService = inject(SnackbarService);

  $users = input.required<AdminUserListItemDto[]>();
  $courses = input.required<AdminCourseDto[]>();
  $isLoading = input<boolean>(false);

  $isLoadingEnrollmentData = signal(false);
  $isLoadingModules = signal(false);
  $selectedCourseId = signal<string>('');
  $selectedModuleId = signal<string>('');
  $selectedUserIds = signal<Set<string>>(new Set());
  $userSearchQuery = signal('');
  $moduleSearchQuery = signal('');
  $modules = signal<AdminModuleDto[]>([]);
  $activeTab = signal<'student' | 'staff'>('student');

  $filteredUsers = computed(() => {
    const query = this.$userSearchQuery().toLowerCase();
    const users = this.$users().filter(u => this.$activeTab() === 'student' ? !!u.student?.id : !!u.staff?.id);

    if (!query) return users;

    return users.filter(user => {
      const fullName = `${user.firstName || ''} ${user.lastName || ''}`.toLowerCase();
      return user.email?.toLowerCase().includes(query) ||
        user.firstName?.toLowerCase().includes(query) ||
        user.lastName?.toLowerCase().includes(query) ||
        fullName.includes(query);
    });
  });

  $filteredStudents = this.$filteredUsers;

  $filteredModules = computed(() => {
    const query = this.$moduleSearchQuery().toLowerCase();
    const modules = this.$modules();

    if (!query) return modules;

    return modules.filter(module =>
      module.moduleCode?.toLowerCase().includes(query) ||
      module.title?.toLowerCase().includes(query)
    );
  });

  selectCourse(courseId: string): void {
    this.$selectedCourseId.set(courseId);
    this.$selectedModuleId.set('');
    this.loadModulesForCourse(courseId);
  }

  setActiveTab(tab: 'student' | 'staff'): void {
    if (this.$activeTab() === tab) return;
    this.$activeTab.set(tab);
    this.resetSelectionState();
  }

  canEnrollInModule(): boolean {
    return this.$selectedUserIds().size > 0 && !!this.$selectedModuleId();
  }

  isStudentSelected(studentId: string): boolean {
    return this.$selectedUserIds().has(studentId);
  }

  toggleStudentSelection(studentId: string): void {
    const selected = new Set(this.$selectedUserIds());
    if (selected.has(studentId)) {
      selected.delete(studentId);
    } else {
      selected.add(studentId);
    }
    this.$selectedUserIds.set(selected);
  }

  selectModuleForEnrollment(moduleId: string): void {
    this.$selectedModuleId.set(moduleId);
  }

  getSelectedUserIds(): string[] {
    return Array.from(this.$selectedUserIds());
  }

  getSelectedUserLabel(): string {
    return this.$activeTab() === 'student' ? 'Student(s)' : 'Staff member(s)';
  }

  getSelectUserHeading(): string {
    return this.$activeTab() === 'student' ? 'Select Students' : 'Select Staff';
  }

  getSearchPlaceholder(): string {
    return this.$activeTab() === 'student'
      ? 'Search students by name or email...'
      : 'Search staff by name or email...';
  }

  getLoadingUsersText(): string {
    return this.$activeTab() === 'student' ? 'Loading students...' : 'Loading staff...';
  }

  getNoUsersFoundText(): string {
    return this.$activeTab() === 'student' ? 'No students found' : 'No staff found';
  }

  loadModulesForCourse(courseId: string): void {
    if (!courseId) {
      this.$modules.set([]);
      return;
    }

    this.$isLoadingModules.set(true);
    this.adminCatalogService.apiAdminCoursesCourseIdModulesGet(courseId).subscribe({
      next: (response) => {
        const moduleData = response.data;
        if (Array.isArray(moduleData)) {
          this.$modules.set(moduleData);
        } else {
          this.$modules.set([]);
        }
        this.$isLoadingModules.set(false);
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
        this.$modules.set([]);
        this.$isLoadingModules.set(false);
      }
    });
  }

  enrollStudentsInModule(): void {
    if (!this.canEnrollInModule()) {
      const target = this.$activeTab() === 'student' ? 'students' : 'staff members';
      this.snackbarService.show(`Please select ${target} and a module`, 400);
      return;
    }

    this.$isLoadingEnrollmentData.set(true);
    const userIds = this.getSelectedUserIds();
    const moduleId = this.$selectedModuleId();
    let successCount = 0;
    let errorCount = 0;

    userIds.forEach(async (userId) => {
      const user = this.$users().find(u => u.id === userId);
      const personId = this.$activeTab() === 'student' ? user?.student?.id : user?.staff?.id;

      if (!personId) {
        errorCount++;
        this.checkEnrollmentComplete(successCount, errorCount, userIds.length);
        return;
      }

      try {
        const enrollmentData = await this.fetchEnrollmentData(personId);
        const params = this.getEnrollmentParams(enrollmentData);

        const request$ = this.$activeTab() === 'student'
          ? this.adminEnrollmentService.apiAdminStudentsStudentIdModulesModuleIdEnrollPost(
            personId,
            moduleId,
            {
              academicYear: params.academicYear,
              yearOfStudy: params.yearOfStudy,
              semester: params.semester
            }
          )
          : this.adminEnrollmentService.apiAdminStaffStaffIdModulesModuleIdEnrollPost(
            personId,
            moduleId,
            {
              academicYear: params.academicYear,
              yearOfStudy: params.yearOfStudy,
              semester: params.semester
            }
          );

        request$.subscribe({
          next: () => {
            successCount++;
            this.checkEnrollmentComplete(successCount, errorCount, userIds.length);
          },
          error: () => {
            errorCount++;
            this.checkEnrollmentComplete(successCount, errorCount, userIds.length);
          }
        });
      } catch {
        errorCount++;
        this.checkEnrollmentComplete(successCount, errorCount, userIds.length);
      }
    });
  }

  private async fetchEnrollmentData(personId: string): Promise<any> {
    if (this.$activeTab() === 'staff') {
      // Staff enrollment should not depend on history endpoint.
      return Promise.resolve(null);
    }

    return new Promise((resolve, reject) => {
      this.enrollmentQueriesService.apiAdminEnrollmentsStudentsStudentIdHistoryGet(personId).subscribe({
        next: (response) => resolve(response.data),
        error: (err) => reject(err)
      });
    });
  }

  private getEnrollmentParams(enrollmentData: any): { academicYear: number; yearOfStudy: number; semester: number } {
    const currentYear = new Date().getFullYear();
    return {
      academicYear: enrollmentData?.academicYear || currentYear,
      yearOfStudy: enrollmentData?.yearOfStudy || 1,
      semester: enrollmentData?.semester || 1
    };
  }

  private checkEnrollmentComplete(successCount: number, errorCount: number, total: number): void {
    if (successCount + errorCount === total) {
      this.$isLoadingEnrollmentData.set(false);
      if (successCount > 0) {
        const label = this.$activeTab() === 'student' ? 'student(s)' : 'staff member(s)';
        this.snackbarService.show(`Successfully enrolled ${successCount} ${label}`, 200);
      }
      if (errorCount > 0) {
        const label = this.$activeTab() === 'student' ? 'student(s)' : 'staff member(s)';
        this.snackbarService.show(`Failed to enroll ${errorCount} ${label}`, 400);
      }
      this.resetSelectionState();
    }
  }

  private resetSelectionState(): void {
    this.$selectedUserIds.set(new Set());
    this.$selectedModuleId.set('');
    this.$selectedCourseId.set('');
    this.$modules.set([]);
    this.$userSearchQuery.set('');
  }
}
