import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {
  AssessmentGradeDto,
  ModuleDto,
  ModuleElementDto,
  ModuleElementType,
  ModuleMemberDto,
  ModuleService
} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {ContextMenuService} from '../../../services/context-menu.service';
import {ContextMenuComponent} from '../../context-menu/context-menu.component';

interface GradeCell {
  grade: AssessmentGradeDto | null;
  editValue: string;
  editFeedback: string;
  dirty: boolean;
  saving: boolean;
}

interface GradingRow {
  student: ModuleMemberDto;
  cells: GradeCell[];
}

interface AssessmentColumn {
  element: ModuleElementDto;
  name: string;
  weight: number | null;
  marksPublished: boolean;
  publishing: boolean;
}

@Component({
  selector: 'app-module-grading',
  standalone: true,
  imports: [CommonModule, MatIconModule, FormsModule, ContextMenuComponent],
  templateUrl: './module-grading.component.html',
  styleUrl: './module-grading.component.scss'
})
export class ModuleGradingComponent implements OnInit {
  private readonly moduleService = inject(ModuleService);
  private readonly snackbar = inject(SnackbarService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  public readonly contextMenu = inject(ContextMenuService);

  $module = signal<ModuleDto | null>(null);
  $isLoading = signal(true);
  $isLoadingGrades = signal(false);
  $error = signal<string | null>(null);
  $assessments = signal<AssessmentColumn[]>([]);
  $rows = signal<GradingRow[]>([]);
  $selectedColumnIdx = signal(0);
  $selectedColumn = computed(() => this.$assessments()[this.$selectedColumnIdx()] ?? null);

  $bulkValue = signal('');
  $bulkAssessmentIdx = signal<number | null>(null);
  $showBulkPanel = signal(false);

  $anyDirty = computed(() => this.$rows().some(r => r.cells.some(c => c.dirty)));
  $contextMenu = this.contextMenu.$state;

  ngOnInit(): void {
    const moduleId = this.route.snapshot.paramMap.get('id');
    if (moduleId) this.loadModule(moduleId);
  }

  loadModule(id: string): void {
    this.$isLoading.set(true);
    this.moduleService.apiModulesIdGet(id).subscribe({
      next: (res) => {
        this.$isLoading.set(false);
        const module = res.data ?? null;
        this.$module.set(module);
        if (module) this.buildGrid(module);
      },
      error: () => {
        this.$isLoading.set(false);
        this.$error.set('Failed to load module.');
      }
    });
  }

  buildGrid(module: ModuleDto): void {
    const assessments: AssessmentColumn[] = (module.elements ?? [])
      .filter(e => e.type === ModuleElementType.NUMBER_4)
      .map(e => ({
        element: e,
        name: e.options?.['name'] || 'Assessment',
        weight: e.assessmentWeight ?? null,
        marksPublished: e.marksPublished ?? false,
        publishing: false
      }));
    this.$assessments.set(assessments);

    const students = module.students ?? [];
    const rows: GradingRow[] = students.map(s => ({
      student: s,
      cells: assessments.map(() => ({grade: null, editValue: '', editFeedback: '', dirty: false, saving: false}))
    }));
    this.$rows.set(rows);

    this.$selectedColumnIdx.set(0);

    if (assessments.length > 0 && students.length > 0) {
      this.loadAllGrades(module.id!, assessments, rows);
    }
  }

  loadAllGrades(moduleId: string, assessments: AssessmentColumn[], rows: GradingRow[]): void {
    this.$isLoadingGrades.set(true);
    let remaining = assessments.length;

    assessments.forEach((col, colIdx) => {
      this.moduleService.apiModulesModuleIdAssessmentsAssessmentElementIdGradesGet(moduleId, col.element.id!).subscribe({
        next: (res) => {
          const grades = res.data ?? [];
          rows.forEach(row => {
            const grade = grades.find(g => g.studentId === row.student.studentId) ?? null;
            row.cells[colIdx].grade = grade;
            row.cells[colIdx].editValue = grade?.grade?.toString() ?? '';
            row.cells[colIdx].editFeedback = grade?.feedback ?? '';
          });

          if (--remaining === 0) {
            this.$isLoadingGrades.set(false);
            this.$rows.update(r => [...r]);
          }
        },
        error: () => {
          if (--remaining === 0) {
            this.$isLoadingGrades.set(false);
          }
        }
      });
    });
  }

  markDirty(row: GradingRow, cellIdx: number): void {
    row.cells[cellIdx].dirty = true;
    this.$rows.update(r => [...r]);
  }

  saveCell(row: GradingRow, cellIdx: number): void {
    const cell = row.cells[cellIdx];
    const col = this.$assessments()[cellIdx];
    const moduleId = this.$module()!.id!;
    const val = String(cell.editValue ?? '').trim();

    if (val === '' && cell.grade == null) {
      cell.dirty = false;
      this.$rows.update(r => [...r]);
      return;
    }

    if (val === '' && cell.grade?.id) {
      cell.saving = true;
      this.$rows.update(r => [...r]);
      this.moduleService.apiModulesModuleIdGradesGradeIdDelete(moduleId, cell.grade.id).subscribe({
        next: () => {
          cell.grade = null;
          cell.dirty = false;
          cell.saving = false;
          this.$rows.update(r => [...r]);
        },
        error: () => {
          cell.saving = false;
          this.$rows.update(r => [...r]);
          this.snackbar.show('Failed to remove grade', 500);
        }
      });

      return;
    }

    const gradeNum = parseFloat(val);

    if (isNaN(gradeNum) || gradeNum < 0 || gradeNum > 100) {
      this.snackbar.show('Grade must be 0–100', 400);
      return;
    }

    cell.saving = true;
    this.$rows.update(r => [...r]);
    this.moduleService.apiModulesModuleIdAssessmentsAssessmentElementIdGradesStudentIdPut(
      moduleId, col.element.id!, row.student.studentId!, {grade: gradeNum, feedback: cell.editFeedback || null}
    ).subscribe({
      next: (res) => {
        cell.grade = res.data ?? cell.grade;
        cell.editValue = cell.grade?.grade?.toString() ?? '';
        cell.dirty = false;
        cell.saving = false;
        this.$rows.update(r => [...r]);
      },
      error: (err) => {
        cell.saving = false;
        this.$rows.update(r => [...r]);
        this.snackbar.show('Failed to save grade', 500, err.error.message);
      }
    });
  }

  saveRow(row: GradingRow): void {
    row.cells.forEach((cell, idx) => {
      if (cell.dirty) this.saveCell(row, idx);
    });
  }

  saveAll(): void {
    this.$rows().forEach(row => this.saveRow(row));
  }

  openBulkPanel(colIdx: number): void {
    this.$bulkAssessmentIdx.set(colIdx);
    this.$bulkValue.set('');
    this.$showBulkPanel.set(true);
  }

  applyBulkFill(): void {
    const colIdx = this.$bulkAssessmentIdx();
    const val = this.$bulkValue().trim();

    if (colIdx == null || val === '') return;

    const gradeNum = parseFloat(val);

    if (isNaN(gradeNum) || gradeNum < 0 || gradeNum > 100) {
      this.snackbar.show('Grade must be 0–100', 400);
      return;
    }

    this.$rows().forEach(row => {
      if (row.cells[colIdx].grade == null) {
        row.cells[colIdx].editValue = val;
        row.cells[colIdx].dirty = true;
      }
    });

    this.$rows.update(r => [...r]);
    this.$showBulkPanel.set(false);
    this.snackbar.show('Applied to ungraded students — save to confirm', 200);
  }

  fillAll(colIdx: number, val: string): void {
    this.$rows().forEach(row => {
      row.cells[colIdx].editValue = val;
      row.cells[colIdx].dirty = true;
    });

    this.$rows.update(r => [...r]);
  }

  clearColumn(colIdx: number): void {
    this.$rows().forEach(row => {
      row.cells[colIdx].editValue = '';
      row.cells[colIdx].dirty = row.cells[colIdx].grade != null;
    });

    this.$rows.update(r => [...r]);
  }

  openRowContextMenu(event: MouseEvent, row: GradingRow, ri: number): void {
    this.contextMenu.open(event, [
      {
        label: 'Save this row', icon: 'save',
        action: () => this.saveRow(row),
        disabled: !row.cells.some(c => c.dirty)
      },
      {
        label: '', divider: true, action: () => {
        }
      },
      {
        label: 'Copy grades down from above', icon: 'arrow_downward',
        disabled: ri === 0,
        action: () => {
          const rows = this.$rows();
          if (ri === 0) return;
          const above = rows[ri - 1];
          row.cells.forEach((cell, i) => {
            cell.editValue = above.cells[i].editValue;
            cell.dirty = true;
          });
          this.$rows.update(r => [...r]);
        }
      },
      {
        label: 'Clear all grades for student', icon: 'clear', danger: true,
        action: () => {
          row.cells.forEach((_, i) => {
            row.cells[i].editValue = '';
            row.cells[i].dirty = row.cells[i].grade != null;
          });
          this.$rows.update(r => [...r]);
        }
      }
    ]);
  }


  publish(col: AssessmentColumn): void {
    const moduleId = this.$module()!.id!;
    col.publishing = true;
    this.moduleService.apiModulesModuleIdPublishMarksPost(moduleId, true).subscribe({
      next: () => {
        col.marksPublished = true;
        col.publishing = false;
        this.$assessments.update(a => [...a]);
        this.snackbar.show('Marks published', 200);
      },
      error: () => {
        col.publishing = false;
        this.snackbar.show('Failed to publish marks', 500);
      }
    });
  }

  openColContextMenu(event: MouseEvent, col: AssessmentColumn, colIdx: number): void {
    this.contextMenu.open(event, [
      {
        label: 'Fill missing with…', icon: 'format_color_fill',
        action: () => this.openBulkPanel(colIdx)
      },
      {
        label: 'Set all to 0', icon: 'exposure_zero',
        action: () => {
          this.fillAll(colIdx, '0');
        }
      },
      {
        label: 'Set all to 100', icon: 'done_all',
        action: () => {
          this.fillAll(colIdx, '100');
        }
      },
      {
        label: 'Clear entire column', icon: 'delete_sweep', danger: true,
        action: () => this.clearColumn(colIdx)
      }
    ]);
  }

  gradedCount(colIdx: number): number {
    return this.$rows().filter(r => r.cells[colIdx]?.grade != null).length;
  }

  goBack(): void {
    const id = this.$module()?.id;
    this.router.navigate(['/modules', id]);
  }
}
