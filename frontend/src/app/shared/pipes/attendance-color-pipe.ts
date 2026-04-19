import {Pipe, PipeTransform} from '@angular/core';

@Pipe({
  name: 'attendanceColor',
  standalone: true
})
export class AttendanceColorPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null) {
      return "#e5e7eb";
    }

    const v = Math.max(0, Math.min(100, value));

    if (v < 60) {
      return "#bc4343";
    }

    if (v < 75) {
      return "#d07b7b";
    }

    if (v < 90) {
      return "#609865";
    }

    return "#4b7550"
  }
}
