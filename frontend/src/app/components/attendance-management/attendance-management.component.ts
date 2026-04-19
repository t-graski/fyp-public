import {Component, computed, effect, inject, signal} from '@angular/core';
import {Card} from '../card/card';
import {MatIconModule} from '@angular/material/icon';
import {Router} from '@angular/router';
import {AttendanceService} from '../../api';
import {MyAttendanceResponseDto} from '../../api';
import {AttendanceOverview} from './components/attendance-overview/attendance-overview';
import {ModuleSummary} from './components/module-summary/module-summary';
import {DailyAttendance} from './components/daily-attendance/daily-attendance';
import {DateRangePicker, DateRange} from './components/date-range-picker/date-range-picker';
import {catchError, of} from 'rxjs';

@Component({
  selector: 'app-attendance-management',
  imports: [
    Card,
    MatIconModule,
    AttendanceOverview,
    ModuleSummary,
    DailyAttendance,
    DateRangePicker
  ],
  templateUrl: './attendance-management.html',
  styleUrl: './attendance-management.component.scss',
})
export class AttendanceManagement {
  private readonly attendanceService = inject(AttendanceService);
  private readonly router = inject(Router);

  $dateRange = signal<DateRange>({
    from: this.getDefaultFromDate(),
    to: this.getDefaultToDate()
  });

  $currentPage = signal(1);
  $pageSize = signal(20);

  $attendanceData = signal<MyAttendanceResponseDto | null>(null);
  $isLoading = signal(false);
  $error = signal<string | null>(null);

  $overview = computed(() => this.$attendanceData()?.overview ?? null);
  $modules = computed(() => this.$attendanceData()?.overview?.perModule ?? []);
  $days = computed(() => this.$attendanceData()?.days?.items ?? []);
  $totalDays = computed(() => this.$attendanceData()?.days?.total ?? 0);
  $totalPages = computed(() => {
    const total = this.$totalDays();
    const size = this.$pageSize();
    return total > 0 ? Math.ceil(total / size) : 0;
  });
  $hasMorePages = computed(() => {
    const currentPage = this.$currentPage();
    const totalPages = this.$totalPages();
    return currentPage < totalPages;
  });

  constructor() {
  effect(() => this.loadAttendanceData(this.$dateRange(), this.$currentPage()));
  }

  private getDefaultFromDate(): string {
    const date = new Date();
    date.setDate(date.getDate() - 30); // Last 30 days
    return date.toISOString().split('T')[0];
  }

  private getDefaultToDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  loadAttendanceData(range: DateRange, page: number) {
    this.$isLoading.set(true);
    this.$error.set(null);

    this.attendanceService.apiAttendanceMeGet(
      range.from,
      range.to,
      page,
      this.$pageSize()
    ).pipe(
      catchError(err => {
        this.$error.set('Failed to load attendance data. Please try again.');
        console.error('Error loading attendance:', err);
        return of(null);
      })
    ).subscribe(response => {
      this.$isLoading.set(false);
      if (response?.data) {
        this.$attendanceData.set(response.data);
      }
    });
  }

  loadNextPage() {
    if (this.$hasMorePages() && !this.$isLoading()) {
      this.$currentPage.update(p => p + 1);
    }
  }

  loadPreviousPage() {
    if (this.$currentPage() > 1 && !this.$isLoading()) {
      this.$currentPage.update(p => p - 1);
    }
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}

