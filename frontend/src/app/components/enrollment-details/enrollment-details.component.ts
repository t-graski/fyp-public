import {Component, computed, effect, inject, input, output, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {EnrollmentQueriesService} from '../../api';
import {SnackbarService} from '../../services/snackbar.service';
import {
  CourseEnrollmentRowDto,
  ModuleEnrollmentRowDto,
  StudentEnrollmentHistoryDto
} from '../../api';

type ViewMode = 'user' | 'course' | 'module';

@Component({
  selector: 'app-enrollment-details',
  imports: [CommonModule, MatIconModule],
  templateUrl: './enrollment-details.component.html',
  styleUrl: './enrollment-details.component.scss'
})
export class EnrollmentDetailsComponent {
  private readonly enrollmentQueriesService = inject(EnrollmentQueriesService);
  private readonly snackbarService = inject(SnackbarService);

  $mode = input.required<ViewMode>();
  $entityId = input.required<string>();
  $entityName = input<string>('');

  close = output<void>();
  enrollStudents = output<{ mode: 'course' | 'module', id: string }>();
  cancelEnrollment = output<{ enrollmentId: string, type: 'course' | 'module' }>();

  $isLoading = signal(false);
  $contextMenuPosition = signal<{ x: number; y: number } | null>(null);
  $contextMenuEnrollmentId = signal<string | null>(null);
  $contextMenuType = signal<'course' | 'module' | null>(null);
  $userEnrollmentData = signal<StudentEnrollmentHistoryDto | null>(null);
  $courseEnrollmentData = signal<CourseEnrollmentRowDto[]>([]);
  $moduleEnrollmentData = signal<ModuleEnrollmentRowDto[]>([]);

  $title = computed(() => {
    switch (this.$mode()) {
      case 'user':
        return 'Student Enrollment History';
      case 'course':
        return 'Course Enrollments';
      case 'module':
        return 'Module Enrollments';
    }
  });

  constructor() {
    effect(() => {
      const mode = this.$mode();
      const id = this.$entityId();
      if (mode && id) {
        this.loadData();
      }
    });
  }

  loadData(): void {
    this.$isLoading.set(true);
    const mode = this.$mode();
    const id = this.$entityId();

    switch (mode) {
      case 'user':
        this.enrollmentQueriesService.apiAdminEnrollmentsStudentsStudentIdHistoryGet(id).subscribe({
          next: (response) => {
            this.$userEnrollmentData.set(response.data || null);
            this.$isLoading.set(false);
          },
          error: (err) => {
            this.snackbarService.showFromApiResponse(err.error);
            this.$isLoading.set(false);
          }
        });
        break;

      case 'course':
        this.enrollmentQueriesService.apiAdminEnrollmentsCoursesCourseIdStudentsGet(id).subscribe({
          next: (response) => {
            this.$courseEnrollmentData.set((response.data as CourseEnrollmentRowDto[]) || []);
            this.$isLoading.set(false);
          },
          error: (err) => {
            this.snackbarService.showFromApiResponse(err.error);
            this.$isLoading.set(false);
          }
        });
        break;

      case 'module':
        this.enrollmentQueriesService.apiAdminEnrollmentsModulesModuleIdStudentsGet(id).subscribe({
          next: (response) => {
            this.$moduleEnrollmentData.set((response.data as ModuleEnrollmentRowDto[]) || []);
            this.$isLoading.set(false);
          },
          error: (err) => {
            this.snackbarService.showFromApiResponse(err.error);
            this.$isLoading.set(false);
          }
        });
        break;
    }
  }

  onClose(): void {
    this.close.emit();
  }

  copyToClipboard(text: string | null | undefined, label: string): void {
    if (!text) {
      this.snackbarService.show('Nothing to copy', 400);
      return;
    }

    navigator.clipboard.writeText(text).then(() => {
      this.snackbarService.show(`${label} copied to clipboard`, 200);
    }).catch(() => {
      this.snackbarService.show('Failed to copy to clipboard', 500);
    });
  }

  getStatusClass(status: number | undefined): string {
    switch (status) {
      case 0:
        return 'status-pending';
      case 1:
        return 'status-active';
      case 2:
        return 'status-completed';
      case 3:
        return 'status-withdrawn';
      case 4:
        return 'status-failed';
      default:
        return '';
    }
  }

  getStatusLabel(status: number | undefined): string {
    switch (status) {
      case 0:
        return 'Pending';
      case 1:
        return 'Active';
      case 2:
        return 'Completed';
      case 3:
        return 'Withdrawn';
      case 4:
        return 'Failed';
      default:
        return 'Unknown';
    }
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString();
  }

  openContextMenu(event: MouseEvent, enrollmentId: string, type: 'course' | 'module'): void {
    event.preventDefault();
    event.stopPropagation();
    this.$contextMenuEnrollmentId.set(enrollmentId);
    this.$contextMenuType.set(type);
    this.$contextMenuPosition.set({x: event.clientX, y: event.clientY});
  }

  closeContextMenu(): void {
    this.$contextMenuEnrollmentId.set(null);
    this.$contextMenuType.set(null);
    this.$contextMenuPosition.set(null);
  }

  handleCancelEnrollment(): void {
    const enrollmentId = this.$contextMenuEnrollmentId();
    const type = this.$contextMenuType();

    if (enrollmentId && type) {
      this.cancelEnrollment.emit({enrollmentId, type});
      this.closeContextMenu();
    }
  }

  handleEnrollStudents(): void {
    const mode = this.$mode();
    const id = this.$entityId();

    if (mode === 'course' || mode === 'module') {
      this.enrollStudents.emit({mode, id});
    }
    this.onClose();
  }
}

