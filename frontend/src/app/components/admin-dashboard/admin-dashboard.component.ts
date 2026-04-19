import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {AdminCatalogService} from '../../api';
import {AdminUserService} from '../../api';
import {UserService} from '../../services/user.service';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {AdminUserListItemDto} from '../../api';
import {AdminCourseDto} from '../../api';
import {EnrollmentDetailsComponent} from '../enrollment-details/enrollment-details.component';
import {AdminUsersTabComponent} from './admin-users-tab/admin-users-tab.component';
import {AdminCoursesTabComponent} from './admin-courses-tab/admin-courses-tab.component';
import {AdminModulesTabComponent} from './admin-modules-tab/admin-modules-tab.component';
import {AdminEnrollCourseTabComponent} from './admin-enroll-course-tab/admin-enroll-course-tab.component';
import {AdminEnrollModuleTabComponent} from './admin-enroll-module-tab/admin-enroll-module-tab.component';
import {AdminAuditTabComponent} from './admin-audit-tab/admin-audit-tab.component';
import {AdminLoginAuditTab} from './admin-login-audit-tab/admin-login-audit-tab';
import {AdminRolesTab} from './admin-roles-tab/admin-roles-tab.component';
import {AdminStudentRecordsTabComponent} from './admin-student-records-tab/admin-student-records-tab.component';
import {PermissionService} from '../../services/permission.service';
import {toSignal} from '@angular/core/rxjs-interop';
import {map} from 'rxjs';

type EnrollmentMode = 'user' | 'course' | 'module';

type AdminDashboardTab =
  'users'
  | 'courses'
  | 'modules'
  | 'roles'
  | 'student-records'
  | 'course-enroll'
  | 'module-enroll'
  | 'log'
  | 'user-history';

const VALID_TABS: AdminDashboardTab[] = ['users', 'courses', 'modules', 'roles', 'student-records', 'course-enroll', 'module-enroll', 'log', 'user-history']

@Component({
  selector: 'app-admin-dashboard',
  imports: [
    CommonModule,
    MatIconModule,
    FormsModule,
    EnrollmentDetailsComponent,
    AdminUsersTabComponent,
    AdminCoursesTabComponent,
    AdminModulesTabComponent,
    AdminRolesTab,
    AdminStudentRecordsTabComponent,
    AdminEnrollCourseTabComponent,
    AdminEnrollModuleTabComponent,
    AdminAuditTabComponent,
    AdminLoginAuditTab
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly adminUserService = inject(AdminUserService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  public readonly permissionService = inject(PermissionService);

  $isLoading = signal(false);

  $users = signal<AdminUserListItemDto[]>([]);
  $courses = signal<AdminCourseDto[]>([]);

  $showEnrollmentDetails = signal<boolean>(false);
  $enrollmentDetailsMode = signal<EnrollmentMode>('user');
  $enrollmentDetailsEntityId = signal<string>('');
  $enrollmentDetailsEntityName = signal<string>('');

  private readonly $queryTab = toSignal(
    this.route.queryParamMap.pipe(
      map(p => p.get('tab') as AdminDashboardTab | null)
    )
  );

  $activeTab = computed<AdminDashboardTab>(() => {
    const t = this.$queryTab();
    return t && VALID_TABS.includes(t) ? t : 'users';
  });

  ngOnInit(): void {
    this.checkAdminAccess();

    const urlTab = this.route.snapshot.queryParamMap.get('tab') as AdminDashboardTab | null;
    if (urlTab && VALID_TABS.includes(urlTab)) {
      this.setView(urlTab);
    } else {
      const savedView = localStorage.getItem('adminDashboard_currentView') as AdminDashboardTab;
      if (savedView && VALID_TABS.includes(savedView)) {
        this.setView(savedView);
      } else {
        this.setViewToFirstAvailable();
      }
    }
  }

  checkAdminAccess(): void {
    if (this.permissionService.isSuperAdmin()) {
      return;
    }

    const currentUser = this.userService.getCurrentUser();
    const hasAdminRole = currentUser?.roles?.some(role =>
      role.key?.toLowerCase() === 'admin'
    ) || false;

    if (hasAdminRole) {
      return;
    }

    if (this.permissionService.hasAnyPermission(
      'UserRead',
      'UserWrite',
      'CatalogRead',
      'CatalogWrite',
      'EnrollmentRead',
      'EnrollmentWrite',
      'AuditRead',
      'RoleRead'
    )) {
      return;
    }

    void this.router.navigateByUrl('/dashboard');
  }

  setView(view: AdminDashboardTab): void {
    localStorage.setItem('adminDashboard_currentView', view);

    switch (view) {
      case 'users':
        this.loadUsers();
        break;
      case 'courses':
        this.loadCourses();
        break;
      case 'modules':
        this.loadCourses();
        break;
      case 'roles':
        break;
      case 'student-records':
        break;
      case 'module-enroll':
      case 'course-enroll':
        this.loadUsers();
        this.loadCourses();
        break;
      case 'log':
        // Audit tab loads its own data
        break;
    case 'user-history':
        // Login audit tab loads its own data
        break;
    }

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {tab: view},
      replaceUrl: false
    })
  }

  setViewToFirstAvailable(): void {
    // Check permissions in order and set to first available view
    if (this.permissionService.$canReadUser() || this.permissionService.isSuperAdmin()) {
      this.setView('users');
    } else if (this.permissionService.$canReadCatalog() || this.permissionService.isSuperAdmin()) {
      this.setView('courses');
    } else if (this.permissionService.$canReadRole() || this.permissionService.isSuperAdmin()) {
      this.setView('roles');
    } else if (this.permissionService.$canWriteEnrollment() || this.permissionService.isSuperAdmin()) {
      this.setView('course-enroll');
    } else if (this.permissionService.$canReadAudit() || this.permissionService.isSuperAdmin()) {
      this.setView('log');
    } else {
      this.setView('users');
    }
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
      error: () => {
        this.$users.set([]);
        this.$isLoading.set(false);
      }
    });
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
      error: () => {
        this.$courses.set([]);
        this.$isLoading.set(false);
      }
    });
  }

  handleViewUserEnrollments(userId: string): void {
    const user = this.$users().find(u => u.id === userId);
    if (user) {
      this.$enrollmentDetailsMode.set('user');
      this.$enrollmentDetailsEntityId.set(user.student?.id || '');
      this.$enrollmentDetailsEntityName.set(`${user.firstName} ${user.lastName}`);
      this.$showEnrollmentDetails.set(true);
    }
  }

  handleViewCourseEnrollments(courseId: string): void {
    const course = this.$courses().find(c => c.id === courseId);
    if (course) {
      this.$enrollmentDetailsMode.set('course');
      this.$enrollmentDetailsEntityId.set(courseId);
      this.$enrollmentDetailsEntityName.set(course.title || '');
      this.$showEnrollmentDetails.set(true);
    }
  }

  handleViewModuleEnrollments(moduleId: string): void {
    this.$enrollmentDetailsMode.set('module');
    this.$enrollmentDetailsEntityId.set(moduleId);
    this.$enrollmentDetailsEntityName.set('Module');
    this.$showEnrollmentDetails.set(true);
  }

  closeEnrollmentDetails(): void {
    this.$showEnrollmentDetails.set(false);
  }

  handleEnrollStudents(event: { mode: 'course' | 'module', id: string }): void {
    this.$showEnrollmentDetails.set(false);
    const tab: AdminDashboardTab = event.mode === 'course' ? 'course-enroll' : 'module-enroll';
    this.setView(tab);
  }
}
