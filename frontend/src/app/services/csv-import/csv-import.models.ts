export interface CsvFieldOption {
  label: string;
  value: string;
}

export interface CsvFieldDefinition {
  key: string;
  label: string;
  required?: boolean;
  defaultValue?: string;
  options?: CsvFieldOption[];
}

export interface CsvFieldMapping {
  fieldKey: string;
  csvColumn: string | null;
  defaultValue: string | null;
}

export interface CsvImportResult<T = Record<string, string>> {
  data: T[];
  mappings: CsvFieldMapping[];
  totalRows: number;
  skippedRows: number;
  rowOverrides?: Partial<T>[];
}

export interface CsvImportOptions {
  title?: string;
  delimiter?: string;
  skipEmptyRows?: boolean;
  trimValues?: boolean;
}

export type ReconciliationStatus = 'idle' | 'reconciling' | 'complete' | 'cancelled';

export interface ReconciliationState {
  status: ReconciliationStatus;
  fields: CsvFieldDefinition[];
  csvHeaders: string[];
  currentFieldIndex: number;
  mappings: CsvFieldMapping[];
  rawRows: string[][];
  options: CsvImportOptions;
}
