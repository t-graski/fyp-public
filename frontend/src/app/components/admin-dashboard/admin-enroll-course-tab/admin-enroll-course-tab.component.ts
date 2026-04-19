import {Component, inject, signal, input, computed} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {FormsModule} from '@angular/forms';
import {AdminEnrollmentService} from '../../../api';
import {EnrollmentQueriesService} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {AdminUserListItemDto} from '../../../api';
import {AdminCourseDto} from '../../../api';

@Component({
  selector: 'app-admin-enroll-course-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule, FormsModule],
  templateUrl: './admin-enroll-course-tab.component.html',
  styleUrl: './admin-enroll-course-tab.component.scss'
})
export class AdminEnrollCourseTabComponent {
  private readonly adminEnrollmentService = inject(AdminEnrollmentService);
  private readonly enrollmentQueriesService = inject(EnrollmentQueriesService);
  private readonly snackbarService = inject(SnackbarService);

  $users = input.required<AdminUserListItemDto[]>();
  $courses = input.required<AdminCourseDto[]>();
  $isLoading = input<boolean>(false);

  $isLoadingEnrollmentData = signal(false);
  $selectedCourseId = signal<string>('');
  $selectedStudentIds = signal<Set<string>>(new Set());
  $studentSearchQuery = signal('');
  $courseSearchQuery = signal('');

  $filteredStudents = computed(() => {
    const query = this.$studentSearchQuery().toLowerCase();
    const students = this.$users().filter(u => !!u.student?.id);

    if (!query) return students;

    return students.filter(user => {
      const fullName = `${user.firstName || ''} ${user.lastName || ''}`.toLowerCase();
      return user.email?.toLowerCase().includes(query) ||
        user.firstName?.toLowerCase().includes(query) ||
        user.lastName?.toLowerCase().includes(query) ||
        fullName.includes(query);
    });
  });

  filteredCourses = computed(() => {
    const query = this.$courseSearchQuery().toLowerCase();
    const courses = this.$courses();

    if (!query) return courses;

    return courses.filter(course =>
      course.courseCode?.toLowerCase().includes(query) ||
      course.title?.toLowerCase().includes(query)
    );
  });

  canEnrollInCourse(): boolean {
    return this.$selectedStudentIds().size > 0 && !!this.$selectedCourseId();
  }

  isStudentSelected(studentId: string): boolean {
    return this.$selectedStudentIds().has(studentId);
  }

  toggleStudentSelection(studentId: string): void {
    const selected = new Set(this.$selectedStudentIds());
    if (selected.has(studentId)) {
      selected.delete(studentId);
    } else {
      selected.add(studentId);
    }
    this.$selectedStudentIds.set(selected);
  }

  selectCourseForEnrollment(courseId: string): void {
    this.$selectedCourseId.set(courseId);
  }

  getSelectedStudentIds(): string[] {
    return Array.from(this.$selectedStudentIds());
  }

  enrollStudentsInCourse(): void {
    if (!this.canEnrollInCourse()) {
      this.snackbarService.show('Please select students and a course', 400);
      return;
    }

    this.$isLoadingEnrollmentData.set(true);
    const studentIds = this.getSelectedStudentIds();
    const courseId = this.$selectedCourseId();
    let successCount = 0;
    let errorCount = 0;

    studentIds.forEach(async (userId) => {
      const user = this.$users().find(u => u.id === userId);
      const studentId = user?.student?.id;

      if (!studentId) {
        errorCount++;
        return;
      }

      try {
        const enrollmentData = await this.fetchStudentEnrollmentData(studentId);
        const params = this.getEnrollmentParams(enrollmentData);

        this.adminEnrollmentService.apiAdminStudentsStudentIdCourseEnrollmentPost(
          studentId,
          {
            courseId: courseId,
            academicYear: params.academicYear,
            yearOfStudy: params.yearOfStudy,
            semester: params.semester
          }
        ).subscribe({
          next: () => {
            successCount++;
            this.checkEnrollmentComplete(successCount, errorCount, studentIds.length);
          },
          error: () => {
            errorCount++;
            this.checkEnrollmentComplete(successCount, errorCount, studentIds.length);
          }
        });
      } catch {
        errorCount++;
      }
    });
  }

  private async fetchStudentEnrollmentData(studentId: string): Promise<any> {
    return new Promise((resolve, reject) => {
      this.enrollmentQueriesService.apiAdminEnrollmentsStudentsStudentIdHistoryGet(studentId).subscribe({
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
        this.snackbarService.show(`Successfully enrolled ${successCount} student(s)`, 200);
      }
      if (errorCount > 0) {
        this.snackbarService.show(`Failed to enroll ${errorCount} student(s)`, 400);
      }
      this.$selectedStudentIds.set(new Set());
      this.$selectedCourseId.set('');
    }
  }
}
