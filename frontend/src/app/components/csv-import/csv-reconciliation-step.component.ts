import {
  Component,
  input,
  output,
  signal,
  effect,
  ChangeDetectionStrategy
} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {MatIconModule} from '@angular/material/icon';
import {CsvFieldDefinition, CsvFieldMapping} from '../../services/csv-import/csv-import.models';

@Component({
  selector: 'app-csv-reconciliation-step',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, MatIconModule],
  templateUrl: './csv-reconciliation-step.component.html',
  styleUrl: './csv-reconciliation-step.component.scss'
})
export class CsvReconciliationStepComponent {
  field = input.required<CsvFieldDefinition>();
  csvHeaders = input.required<string[]>();
  currentMapping = input<CsvFieldMapping | null>(null);
  currentIndex = input<number>(0);
  totalCount = input<number>(1);
  progress = input<number>(0);

  next = output<CsvFieldMapping>();
  back = output<void>();

  $selectedColumn = signal<string | null>(null);
  $useDefault = signal(false);
  $defaultValue = signal('');

  constructor() {
    effect(() => {
      const mapping = this.currentMapping();
      this.$selectedColumn.set(mapping?.csvColumn ?? null);
      this.$useDefault.set(mapping?.csvColumn === null && !!mapping?.defaultValue);
      this.$defaultValue.set(mapping?.defaultValue ?? this.field().defaultValue ?? '');
    });
  }

  get canProceed(): boolean {
    const field = this.field();
    if (!field.required) return true;
    if (this.$selectedColumn()) return true;
    if (this.$useDefault()) {
      if (field.options) return !!this.$defaultValue();
      return !!this.$defaultValue().trim();
    }
    return false;
  }

  selectColumn(header: string): void {
    this.$selectedColumn.set(header);
    this.$useDefault.set(false);
  }


  toggleDefault(): void {
    this.$useDefault.update(v => !v);
    if (this.$useDefault()) {
      this.$selectedColumn.set(null);
    }
  }

  handleNext(): void {
    const mapping: CsvFieldMapping = {
      fieldKey: this.field().key,
      csvColumn: this.$useDefault() ? null : (this.$selectedColumn() ?? null),
      defaultValue: this.$useDefault() ? this.$defaultValue().trim() : null
    };
    this.next.emit(mapping);
  }

  handleBack(): void {
    this.back.emit();
  }
}
