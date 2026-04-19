import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {Router} from '@angular/router';
import {UserService} from '../../../services/user.service';
import {MeService, StudentDashboardDto, UserDetailDto} from '../../../api';

@Component({
  selector: 'app-profile-info-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './profile-info-tab.component.html',
  styleUrl: './profile-info-tab.component.scss'
})
export class ProfileInfoTabComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly meService = inject(MeService);
  private readonly router = inject(Router);

  $user = signal<UserDetailDto | null>(null);
  $dashboard = signal<StudentDashboardDto | null>(null);
  $isLoading = signal(true);

  ngOnInit(): void {
    this.loadAllData();
  }

  private loadAllData(): void {
    this.$isLoading.set(true);

    this.userService.loadCurrentUser().subscribe({
      next: (user) => {
        this.$user.set(user);

        const isStudent = user.roles?.some(r => r.key?.toLowerCase() === 'student') ?? false;

        if (isStudent) {
          this.meService.apiMeDashboardGet().subscribe({
            next: (dashboard) => {
              this.$dashboard.set(dashboard?.data ?? null);
              this.$isLoading.set(false);
            }
          })
        } else {
          this.$isLoading.set(false);
        }
      },
      error: (err) => {
        console.error('Failed to load user info', err);
        this.$isLoading.set(false);
      }
    });
  }

  get fullName(): string {
    const u = this.$user();
    if (!u) return 'N/A';
    return `${u.firstName || ''} ${u.lastName || ''}`.trim() || 'N/A';
  }

  getRoleBadgeClass(roleKey: string | null | undefined): string {
    const map: Record<string, string> = {
      student: 'profile-info-role-badge--student',
      staff: 'profile-info-role-badge--staff',
      admin: 'profile-info-role-badge--admin',
    };
    return map[roleKey?.toLowerCase() ?? ''] ?? '';
  }

  getCourseEnrollmentStatus(status: string | null | undefined): string {
    switch (status) {
      case "1":
        return "Active"
      case "2":
        return "Completed"
      case "3":
        return "Withdrawn"
      default:
        return "Unknown"
    }
  }

  navigateToDashboard(): void {
    void this.router.navigateByUrl('/dashboard');
  }

  navigateToModule(moduleId: string): void {
    void this.router.navigateByUrl(`/modules/${moduleId}`);
  }
}
