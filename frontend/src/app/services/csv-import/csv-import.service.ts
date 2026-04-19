import {Injectable, ApplicationRef, createComponent, EnvironmentInjector, inject, ComponentRef} from '@angular/core';
import {Observable, Subject} from 'rxjs';
import {CsvFieldDefinition, CsvImportOptions, CsvImportResult, ReconciliationState} from './csv-import.models';
import {CsvImportOverlayComponent} from '../../components/csv-import/csv-import-overlay.component';

@Injectable({
  providedIn: 'root'
})
export class CsvImportService {
  private readonly appRef = inject(ApplicationRef);
  private readonly injector = inject(EnvironmentInjector);

  private overlayRef: ComponentRef<CsvImportOverlayComponent> | null = null;

  import<T = Record<string, string>>(
    fields: CsvFieldDefinition[],
    options: CsvImportOptions = {}
  ): Observable<CsvImportResult<T>> {
    const result$ = new Subject<CsvImportResult<T>>();

    const overlayRef = createComponent(CsvImportOverlayComponent, {
      environmentInjector: this.injector
    });

    this.overlayRef = overlayRef;
    overlayRef.instance.fields = fields;
    overlayRef.instance.options = options;

    overlayRef.instance.onComplete.subscribe((result: CsvImportResult) => {
      result$.next(result as CsvImportResult<T>);
      result$.complete();
      this.destroyOverlay();
    });

    overlayRef.instance.onCancel.subscribe(() => {
      result$.complete();
      this.destroyOverlay();
    });

    this.appRef.attachView(overlayRef.hostView);
    document.body.appendChild(overlayRef.location.nativeElement);
    overlayRef.changeDetectorRef.detectChanges();

    return result$.asObservable();
  }

  private destroyOverlay(): void {
    if (this.overlayRef) {
      this.appRef.detachView(this.overlayRef.hostView);
      this.overlayRef.location.nativeElement.remove();
      this.overlayRef.destroy();
      this.overlayRef = null;
    }
  }

  parseCsv(content: string, delimiter = ','): { headers: string[]; rows: string[][] } {
    const lines = content.split(/\r?\n/);
    const headers = this.parseCsvLine(lines[0] ?? '', delimiter);
    const rows: string[][] = [];

    for (let i = 1; i < lines.length; i++) {
      const line = lines[i].trim();
      if (!line) continue;
      rows.push(this.parseCsvLine(line, delimiter));
    }

    return {headers, rows};
  }

  parseCsvLine(line: string, delimiter = ','): string[] {
    const result: string[] = [];
    let current = '';
    let inQuotes = false;

    for (let i = 0; i < line.length; i++) {
      const char = line[i];

      if (char === '"') {
        if (inQuotes && line[i + 1] === '"') {
          current += '"';
          i++;
        } else {
          inQuotes = !inQuotes;
        }
      } else if (char === delimiter && !inQuotes) {
        result.push(current.trim());
        current = '';
      } else {
        current += char;
      }
    }

    result.push(current.trim());
    return result;
  }

  buildResult<T = Record<string, string>>(state: ReconciliationState): CsvImportResult<T> {
    const {mappings, rawRows, options} = state;
    const trimValues = options.trimValues ?? true;
    const skipEmptyRows = options.skipEmptyRows ?? true;

    const data: T[] = [];
    let skippedRows = 0;

    for (const row of rawRows) {
      if (skipEmptyRows && row.every(cell => cell.trim() === '')) {
        skippedRows++;
        continue;
      }

      const entry: Record<string, string> = {};

      for (const mapping of mappings) {
        let value: string;

        if (mapping.csvColumn !== null) {
          const colIndex = state.csvHeaders.indexOf(mapping.csvColumn);
          value = colIndex >= 0 && row[colIndex] !== undefined ? row[colIndex] : '';
        } else {
          value = mapping.defaultValue ?? '';
        }

        if (trimValues) value = value.trim();
        entry[mapping.fieldKey] = value;
      }

      data.push(entry as T);
    }

    return {
      data,
      mappings,
      totalRows: rawRows.length,
      skippedRows
    };
  }
}
