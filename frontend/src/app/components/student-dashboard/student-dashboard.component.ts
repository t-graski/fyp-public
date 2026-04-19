import {ChangeDetectionStrategy, ChangeDetectorRef, Component, computed, inject, OnInit, signal} from '@angular/core';
import {MeService} from '../../api';
import {StudentDashboardDto} from '../../api';
import {Card} from '../card/card';
import {MatIconModule} from '@angular/material/icon';
import {Router} from '@angular/router';
import {FormsModule} from '@angular/forms';
import {ModuleCardDto} from '../../api';
import {UserService} from '../../services/user.service';
import {forkJoin} from 'rxjs';

@Component({
  selector: 'app-student-dashboard',
  templateUrl: './student-dashboard.component.html',
  styleUrls: ['./student-dashboard.component.scss'],
  imports: [Card, MatIconModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StudentDashboardComponent implements OnInit {
  private readonly meService = inject(MeService);
  private readonly userService = inject(UserService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);

  $dashboardData = signal<StudentDashboardDto | null>(null);
  $studentName = signal<string>('Student');
  $showPreviousModules = signal(false);
  $isLoading = signal(true);
  $searchQuery = signal('');

  $filteredCurrentModules = computed(() => {
    const data = this.$dashboardData();
    const query = this.$searchQuery().toLowerCase().trim();

    if (!data?.currentModules || !query) {
      return data?.currentModules || [];
    }

    return data.currentModules.filter(module =>
      module.title?.toLowerCase().includes(query) ||
      module.moduleCode?.toLowerCase().includes(query)
    );
  });

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.$isLoading.set(true);
    forkJoin({
      dashboard: this.meService.apiMeDashboardGet(),
      user: this.userService.loadCurrentUser()
    }).subscribe({
      next: ({ dashboard, user }) => {
        this.$dashboardData.set(dashboard.data ?? null);
        if (user.email) {
          const emailName = user.email.split('@')[0];
          const formattedName = emailName
            .split('.')
            .map(part => part.charAt(0).toUpperCase() + part.slice(1))
            .join(' ');
          this.$studentName.set(formattedName);
        }
        this.$isLoading.set(false);
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to load dashboard:', err);
        this.$isLoading.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  togglePreviousModules(): void {
    this.$showPreviousModules.update(value => !value);
  }

  navigateToAttendance(): void {
    void this.router.navigateByUrl('/attendance');
  }

  onSearchChange(value: string): void {
    this.$searchQuery.set(value);
  }

  onModuleClick(module: ModuleCardDto): void {
    void this.router.navigate(['/modules', module.moduleId]);
  }
}
