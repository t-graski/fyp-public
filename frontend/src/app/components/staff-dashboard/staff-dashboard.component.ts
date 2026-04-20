import {ChangeDetectionStrategy, ChangeDetectorRef, Component, computed, inject, OnInit, signal} from '@angular/core';
import {MeService, StaffDashboardDto, StaffModuleCardDto} from '../../api';
import {Card} from '../card/card';
import {MatIconModule} from '@angular/material/icon';
import {Router} from '@angular/router';
import {FormsModule} from '@angular/forms';
import {UserService} from '../../services/user.service';
import {forkJoin} from 'rxjs';

@Component({
  selector: 'app-staff-dashboard',
  templateUrl: './staff-dashboard.component.html',
  styleUrls: ['./staff-dashboard.component.scss'],
  imports: [Card, MatIconModule, FormsModule]
})
export class StaffDashboardComponent implements OnInit {
  private readonly meService = inject(MeService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);

  $dashboardData = signal<StaffDashboardDto | null>(null);
  $staffName = signal<string>('Staff Member');
  $isLoading = signal(true);
  $searchQuery = signal('');

  $filteredModules = computed(() => {
    const data = this.$dashboardData();
    const query = this.$searchQuery().toLowerCase().trim();

    if (!data?.modules || !query) {
      return data?.modules || [];
    }

    return data.modules.filter(module =>
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
      dashboard: this.meService.apiMeDashboardStaffGet(),
      user: this.userService.loadCurrentUser()
    }).subscribe({
      next: ({dashboard, user}) => {
        this.$dashboardData.set(dashboard.data ?? null);
        if (user.email) {
          const emailName = user.email.split('@')[0];
          const formattedName = emailName
            .split('.')
            .map(part => part.charAt(0).toUpperCase() + part.slice(1))
            .join(' ');
          this.$staffName.set(formattedName);
        }
        this.$isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load staff dashboard:', err);
        this.$isLoading.set(false);
      }
    });
  }

  onSearchChange(value: string): void {
    this.$searchQuery.set(value);
  }

  onModuleClick(module: StaffModuleCardDto): void {
    void this.router.navigate(['/modules', module.moduleId]);
  }
}
