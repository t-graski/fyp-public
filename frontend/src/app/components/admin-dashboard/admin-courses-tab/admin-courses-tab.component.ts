import {Component, inject, signal, output, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {
  AdminCatalogService,
  CreateCourseDto,
} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {AdminCourseDto} from '../../../api';
import {DynamicTableComponent, TableColumn, TableAction} from '../../dynamic-table/dynamic-table.component';
import {FormsModule} from '@angular/forms';
import {HasPermissionDirective} from '../../../directives/has-permission.directive';

@Component({
  selector: 'app-admin-courses-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule, DynamicTableComponent, FormsModule, HasPermissionDirective],
  templateUrl: './admin-courses-tab.component.html',
  styleUrl: './admin-courses-tab.component.scss'
})
export class AdminCoursesTabComponent implements OnInit {
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly snackbarService = inject(SnackbarService);

  $isLoading = signal(false);
  $courses = signal<AdminCourseDto[]>([]);
  $showCourseDetails = signal<string | null>(null);
  $showCreateCourseModal = signal<boolean>(false);
  $showEditCourseModal = signal<string | null>(null);

  $newCourse = signal<CreateCourseDto>({
    courseCode: '',
    title: '',
    description: '',
    award: '',
    durationSemesters: 0,
    isActive: true
  });

  $editCourseData = signal<AdminCourseDto>({
    id: '',
    courseCode: '',
    title: '',
    description: '',
    award: '',
    durationSemesters: 0,
    isActive: true
  });

  courseColumns: TableColumn<AdminCourseDto>[] = [
    {key: 'id', label: 'ID', visible: false, cellClass: 'cell-id'},
    {key: 'courseCode', label: 'Code', sortable: true, visible: true},
    {key: 'title', label: 'Title', sortable: true, visible: true},
    {key: 'description', label: 'Description', visible: true, cellClass: 'cell-description'},
    {key: 'award', label: 'Award', visible: true},
    {key: 'durationSemesters', label: 'Duration', visible: true},
    {
      key: 'isActive',
      label: 'Status',
      visible: true,
      render: (course) => course.isActive ? 'Active' : 'Inactive'
    }
  ];

  courseActions: TableAction<AdminCourseDto>[] = [
    {
      icon: 'visibility',
      label: 'View Details',
      handler: (course) => this.$showCourseDetails.set(course.id!),
      requiredPermission: 'CatalogRead'
    },
    {
      icon: 'people',
      label: 'View Enrolled Students',
      handler: (course) => this.viewCourseEnrollments(course.id!),
      requiredPermission: 'EnrollmentRead'
    },
    {
      icon: 'edit',
      label: 'Edit Course',
      handler: (course) => this.editCourse(course),
      requiredPermission: 'CatalogWrite'
    },
    {
      icon: 'content_copy',
      label: 'Clone Course',
      handler: (course) => this.cloneCourse(course),
      requiredPermission: 'CatalogWrite'
    },
    {
      divider: true, icon: '', label: '', handler: () => {
      }
    },
    {
      icon: 'delete',
      label: 'Delete Course',
      danger: true,
      handler: (course) => this.deleteCourse(course.id!),
      requiredPermission: 'CatalogDelete'
    }
  ];

  $viewEnrollments = output<string>();

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.$isLoading.set(true);
    this.adminCatalogService.apiAdminCoursesGet().subscribe({
      next: (response) => {
        const courseData = response.data;
        if (Array.isArray(courseData)) {
          this.$courses.set(courseData);
        } else {
          this.$courses.set([]);
        }
        this.$isLoading.set(false);
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
        this.$isLoading.set(false);
      }
    });
  }

  reloadCourses(): void {
    this.loadCourses();
  }

  viewCourseEnrollments(courseId: string): void {
    this.$viewEnrollments.emit(courseId);
  }

  closeCourseDetails(): void {
    this.$showCourseDetails.set(null);
  }

  editCourse(course: AdminCourseDto): void {
    this.$editCourseData.set({
      id: course.id,
      courseCode: course.courseCode || '',
      title: course.title || '',
      description: course.description || '',
      award: course.award || '',
      durationSemesters: course.durationSemesters || 0,
      isActive: course.isActive || true
    });

    this.$showEditCourseModal.set(course.id!)
  }

  cloneCourse(course: AdminCourseDto): void {
    this.$newCourse.set({
      courseCode: `${course.courseCode}-COPY`,
      title: `${course.title} (Copy)`,
      description: course.description || '',
      award: course.award || '',
      durationSemesters: course.durationSemesters || 0,
      isActive: true
    });

    this.$showCreateCourseModal.set(true);
  }

  deleteCourse(courseId: string): void {
    if (!confirm('Are you sure you want to delete this course? This action cannot be undone.')) return;

    this.adminCatalogService.apiAdminCoursesIdDelete(courseId).subscribe({
      next: (response: any) => {
        this.snackbarService.showFromApiResponse(response);
        this.loadCourses();
      },
      error: (err: any) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }

  openCreateCourseModal(): void {
    this.$showCreateCourseModal.set(true);
  }

  closeCreateCourseModal(): void {
    this.$showCreateCourseModal.set(false);
  }

  closeEditCourseModal(): void {
    this.$showEditCourseModal.set(null);
  }

  createCourse(): void {
    const course = this.$newCourse();
    if (!course.courseCode || !course.title) {
      this.snackbarService.show('Please fill required fields', 400);
      return;
    }

    this.adminCatalogService.apiAdminCoursesPost({
      courseCode: course.courseCode,
      title: course.title,
      description: course.description,
      award: course.award,
      durationSemesters: course.durationSemesters,
      isActive: course.isActive
    }).subscribe({
      next: (response) => {
        this.snackbarService.showFromApiResponse(response);
        this.closeCreateCourseModal();
        this.$newCourse.set({
          courseCode: '',
          title: '',
          description: '',
          award: '',
          durationSemesters: 0,
          isActive: true
        });
        this.loadCourses();
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }

  updateCourse(): void {
    const course = this.$editCourseData();
    this.adminCatalogService.apiAdminCoursesIdPut(course.id!, {
      courseCode: course.courseCode,
      title: course.title,
      description: course.description,
      award: course.award,
      durationSemesters: course.durationSemesters,
      isActive: course.isActive
    }).subscribe({
      next: (response: any) => {
        this.snackbarService.showFromApiResponse(response);
        this.closeEditCourseModal();
        this.loadCourses();
      },
      error: (err: any) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }
}
