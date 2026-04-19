import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {MeService} from '../../../api';
import {StudentModuleGradesDto} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';

@Component({
  selector: 'app-profile-grades-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './profile-grades-tab.component.html',
  styleUrl: './profile-grades-tab.component.scss'
})
export class ProfileGradesTabComponent implements OnInit {
  private readonly meService = inject(MeService);
  private readonly snackbarService = inject(SnackbarService);

  $modules = signal<StudentModuleGradesDto[]>([]);
  $isLoading = signal(true);
  $expandedIds = signal<Set<string>>(new Set());


  ngOnInit(): void {
    this.loadGrades();
  }

  private loadGrades(): void {
    this.$isLoading.set(true);
    this.meService.apiMeGradesGet().subscribe({
      next: (response) => {
        const modules = (response.data?.courses ?? []).flatMap(c => c.modules ?? []);
        this.$modules.set(modules);
        this.$isLoading.set(false);
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
        this.$isLoading.set(false);
      }
    });
  }

  toggleModule(id: string): void {
    const next = new Set(this.$expandedIds());
    next.has(id) ? next.delete(id) : next.add(id);
    this.$expandedIds.set(next);
  }

  isExpanded(id: string): boolean {
    return this.$expandedIds().has(id);
  }

  moduleAverage(m: StudentModuleGradesDto): number | null {
    const grades = (m.assessments ?? []).filter(a => a.grade !== undefined).map(a => a.grade!);
    return grades.length ? grades.reduce((a, b) => a + b, 0) / grades.length : null;
  }

  getGradeColor(grade: number): string {
    if (grade >= 70) return '#10b981';
    if (grade >= 50) return '#f59e0b';
    return '#ef4444';
  }

  getGradeLabel(grade: number): string {
    if (grade >= 70) return 'Pass';
    if (grade >= 50) return 'Marginal';
    return 'Fail';
  }

  isCore(m: StudentModuleGradesDto): boolean {
    return (m as any)['isCore'] === true;
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleDateString(undefined, {day: 'numeric', month: 'short', year: 'numeric'});
  }
}
