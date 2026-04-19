import {
  Component,
  signal,
  computed,
  EventEmitter,
  inject,
  OnInit
} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {MatIconModule} from '@angular/material/icon';
import {
  CsvFieldDefinition,
  CsvFieldMapping,
  CsvImportOptions,
  CsvImportResult,
  ReconciliationState
} from '../../services/csv-import/csv-import.models';
import {CsvImportService} from '../../services/csv-import/csv-import.service';
import {CsvReconciliationStepComponent} from './csv-reconciliation-step.component';
import {FindFieldOptionsPipe} from './find-field-options.pipe';
import {ConfirmDialogService} from '../../services/confirm-dialog.service';

type OverlayStep = 'upload' | 'reconciliation' | 'preview';

@Component({
  selector: 'app-csv-import-overlay',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, CsvReconciliationStepComponent, FindFieldOptionsPipe],
  templateUrl: './csv-import-overlay.component.html',
  styleUrl: './csv-import-overlay.component.scss'
})
export class CsvImportOverlayComponent implements OnInit {
  private readonly csvImportService = inject(CsvImportService);
  private readonly confirmDialogService = inject(ConfirmDialogService);

  fields: CsvFieldDefinition[] = [];
  options: CsvImportOptions = {};

  onComplete = new EventEmitter<CsvImportResult>();
  onCancel = new EventEmitter<void>();

  $step = signal<OverlayStep>('upload');
  $isDragOver = signal(false);
  $fileName = signal<string | null>(null);
  $parseError = signal<string | null>(null);

  $reconciliationState = signal<ReconciliationState | null>(null);
  $previewData = signal<Record<string, string>[]>([]);
  $rowOverrides = signal<Record<string, string>[]>([]);
  $previewResult = signal<CsvImportResult | null>(null);

  $title = computed(() => this.options.title ?? 'Import CSV');

  $missingFields = computed(() => {
    const state = this.$reconciliationState();
    if (!state) return [];
    return state.fields.filter(f =>
      !state.csvHeaders.some(h => h.toLowerCase() === f.key.toLowerCase())
    );
  });

  $currentReconciliationField = computed(() => {
    const state = this.$reconciliationState();
    if (!state) return null;
    return this.$missingFields()[state.currentFieldIndex] ?? null;
  });

  $currentReconciliationMapping = computed(() => {
    const field = this.$currentReconciliationField();
    const state = this.$reconciliationState();
    if (!field || !state) return null;
    return state.mappings.find(m => m.fieldKey === field.key) ?? null;
  });

  $reconciliationProgress = computed(() => {
    const missing = this.$missingFields();
    const state = this.$reconciliationState();
    if (!state || missing.length === 0) return 100;
    return Math.round((state.currentFieldIndex / missing.length) * 100);
  });

  $previewColumns = computed(() => this.fields.map(f => f.key));

  $optionFields = computed(() => this.fields.filter(f => f.options && f.options.length > 0));

  $previewPage = signal(0);
  readonly pageSize = 10;

  $pagedPreview = computed(() => {
    const page = this.$previewPage();
    const data = this.$previewData();
    return data.slice(page * this.pageSize, (page + 1) * this.pageSize);
  });

  $totalPages = computed(() => Math.ceil(this.$previewData().length / this.pageSize));

  ngOnInit(): void {
  }

  setRowOverride(absoluteIndex: number, fieldKey: string, value: string): void {
    const overrides = this.$rowOverrides().map((r, i) =>
      i === absoluteIndex ? {...r, [fieldKey]: value} : r
    );
    this.$rowOverrides.set(overrides);
  }

  handleDragOver(event: DragEvent): void {
    event.preventDefault();
    this.$isDragOver.set(true);
  }

  handleDragLeave(): void {
    this.$isDragOver.set(false);
  }

  handleDrop(event: DragEvent): void {
    event.preventDefault();
    this.$isDragOver.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) this.processFile(file);
  }

  handleFileInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.processFile(file);
  }

  private processFile(file: File): void {
    this.$parseError.set(null);

    if (!file.name.endsWith('.csv')) {
      this.$parseError.set('Only CSV files are supported.');
      return;
    }

    this.$fileName.set(file.name);

    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      this.parseAndProceed(content);
    };
    reader.readAsText(file);
  }

  private parseAndProceed(content: string): void {
    try {
      const delimiter = this.options.delimiter ?? ',';
      const {headers, rows} = this.csvImportService.parseCsv(content, delimiter);

      if (headers.length === 0) {
        this.$parseError.set('The CSV file appears to be empty or malformed.');
        return;
      }

      const autoMappings: CsvFieldMapping[] = this.fields.map(field => {
        const matchedHeader = headers.find(
          h => h.toLowerCase() === field.key.toLowerCase() ||
            h.toLowerCase() === field.label.toLowerCase()
        );
        return {
          fieldKey: field.key,
          csvColumn: matchedHeader ?? null,
          defaultValue: field.defaultValue ?? null
        };
      });

      const missingAny = autoMappings.some(m => m.csvColumn === null);

      const state: ReconciliationState = {
        status: missingAny ? 'reconciling' : 'complete',
        fields: this.fields,
        csvHeaders: headers,
        currentFieldIndex: 0,
        mappings: autoMappings,
        rawRows: rows,
        options: this.options
      };

      this.$reconciliationState.set(state);

      if (missingAny) {
        this.$step.set('reconciliation');
      } else {
        this.buildPreview(state);
        this.$step.set('preview');
      }
    } catch {
      this.$parseError.set('Failed to parse the CSV file. Please check the format.');
    }
  }

  handleReconciliationNext(mapping: CsvFieldMapping): void {
    const state = this.$reconciliationState();
    if (!state) return;

    const updatedMappings = state.mappings.map(m =>
      m.fieldKey === mapping.fieldKey ? mapping : m
    );

    const missing = this.$missingFields();
    const nextIndex = state.currentFieldIndex + 1;

    if (nextIndex >= missing.length) {
      const finalState: ReconciliationState = {
        ...state,
        mappings: updatedMappings,
        currentFieldIndex: nextIndex,
        status: 'complete'
      };

      this.$reconciliationState.set(finalState);
      this.buildPreview(finalState);
      this.$step.set('preview');
    } else {
      this.$reconciliationState.set({
        ...state,
        mappings: updatedMappings,
        currentFieldIndex: nextIndex
      });
    }
  }

  handleReconciliationBack(): void {
    const state = this.$reconciliationState();
    if (!state || state.currentFieldIndex === 0) return;
    this.$reconciliationState.set({
      ...state,
      currentFieldIndex: state.currentFieldIndex - 1
    });
  }

  private buildPreview(state: ReconciliationState): void {
    const result = this.csvImportService.buildResult(state);
    this.$previewResult.set(result);
    this.$previewData.set(result.data as Record<string, string>[]);
    this.$rowOverrides.set(Array(result.data.length).fill(null).map(() => ({})));
    this.$previewPage.set(0);
  }

  confirmImport(): void {
    const result = this.$previewResult();

    if (!result) return;

    const rowCount = result.totalRows - result.skippedRows;

    this.confirmDialogService.confirm({
      title: 'Confirm Import',
      message: `You are about to import ${rowCount} row${rowCount !== 1 ? 's' : ''}. This action cannot be undone.`,
      confirmLabel: `Import ${rowCount} row${rowCount !== 1 ? 's' : ''}`,
      cancelLabel: 'Go back'
    }).subscribe(confirmed => {
      if (!confirmed) return;

      const overrides = this.$rowOverrides();
      const mergedData = (result.data as Record<string, string>[]).map((row, i) => ({
        ...row,
        ...(overrides[i] ?? {})
      }));

      this.onComplete.emit({...result, data: mergedData});
    });
  }

  goBack(): void {
    const step = this.$step();

    if (step === 'preview') {
      const state = this.$reconciliationState();

      if (state && this.$missingFields().length > 0) {
        this.$reconciliationState.set({
          ...state,
          currentFieldIndex: this.$missingFields().length - 1,
          status: 'reconciling'
        });
        this.$step.set('reconciliation');
      } else {
        this.$step.set('upload');
      }
    } else if (step === 'reconciliation') {
      this.$step.set('upload');
    }
  }

  cancel(): void {
    this.onCancel.emit();
  }
}
