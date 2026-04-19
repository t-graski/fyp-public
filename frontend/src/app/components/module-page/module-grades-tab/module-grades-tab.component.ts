import {Component, inject, input, OnChanges, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {AssessmentGradeDto, ModuleElementDto, ModuleElementType, ModuleMemberDto, ModuleService} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';

interface AssessmentGradeRow {
  student: ModuleMemberDto;
  grade: AssessmentGradeDto | null;
}

interface AssessmentState {
  elementId: string;
  name: string;
  weight: number | null;
  marksPublished: boolean;
  expanded: boolean;
  loading: boolean;
  rows: AssessmentGradeRow[];
  publishing: boolean;
}

@Component({
  selector: 'app-module-grades-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './module-grades-tab.component.html',
  styleUrl: './module-grades-tab.component.scss'
})
export class ModuleGradesTabComponent implements OnChanges {
  private readonly moduleService = inject(ModuleService);
  private readonly snackbar = inject(SnackbarService);

  $elements = input<ModuleElementDto[]>([]);
  $moduleId = input.required<string>();
  $isStaff = input(false);
  $students = input<ModuleMemberDto[]>([]);
  $assessments = signal<AssessmentState[]>([]);

  ngOnChanges(): void {
    const elements = this.$elements();
    const current = this.$assessments();
    const states: AssessmentState[] = elements
      .filter(e => e.type === ModuleElementType.NUMBER_4)
      .map(e => {
        const existing = current.find(a => a.elementId === e.id);
        return existing ?? {
          elementId: e.id!,
          name: e.options?.['name'] || 'Assessment',
          weight: e.assessmentWeight ?? null,
          marksPublished: e.marksPublished ?? false,
          expanded: false,
          loading: false,
          rows: [],
          publishing: false,
        };
      });

    this.$assessments.set(states);
  }

  toggleExpand(state: AssessmentState): void {
    state.expanded = !state.expanded;

    if (state.expanded && state.rows.length === 0) {
      this.loadGrades(state);
    }
  }

  loadGrades(state: AssessmentState): void {
    state.loading = true;
    this.moduleService.apiModulesModuleIdAssessmentsAssessmentElementIdGradesGet(
      this.$moduleId(), state.elementId
    ).subscribe({
      next: (res) => {
        state.loading = false;
        const grades = res.data ?? [];
        if (this.$isStaff()) {
          state.rows = this.$students().map(student => {
            const grade = grades.find(g => g.studentId === student.studentId) ?? null;
            return {student, grade};
          });
        } else {
          const myGrade = grades[0] ?? null;
          state.rows = myGrade != null ? [{student: this.$students()[0] ?? {} as ModuleMemberDto, grade: myGrade}] : [];
        }
        this.$assessments.update(a => [...a]);
      },
      error: () => {
        state.loading = false;
        this.snackbar.show('Failed to load grades', 500);
        this.$assessments.update(a => [...a]);
      }
    });
  }

  publish(state: AssessmentState): void {
    state.publishing = true;
    this.moduleService.apiModulesModuleIdPublishMarksPost(this.$moduleId(), true).subscribe({
      next: () => {
        state.publishing = false;
        state.marksPublished = true;
        this.$assessments.update(a => [...a]);
        this.snackbar.show('Marks published', 200);
      },
      error: () => {
        state.publishing = false;
        this.snackbar.show('Failed to publish marks', 500);
      }
    });
  }

  gradedCount(state: AssessmentState): number {
    return state.rows.filter(r => r.grade != null).length;
  }
}
