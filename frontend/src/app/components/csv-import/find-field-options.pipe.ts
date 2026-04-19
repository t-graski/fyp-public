import {Pipe, PipeTransform} from '@angular/core';
import {CsvFieldDefinition, CsvFieldOption} from '../../services/csv-import/csv-import.models';

@Pipe({
  name: 'findFieldOptions',
  standalone: true
})
export class FindFieldOptionsPipe implements PipeTransform {
  transform(fields: CsvFieldDefinition[], key: string): CsvFieldOption[] | null {
    const field = fields.find(f => f.key === key);
    return field?.options?.length ? field.options : null;
  }
}
