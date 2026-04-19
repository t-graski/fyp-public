import {Component, input} from '@angular/core';
import {Card} from '../../../card/card';
import {MatIconModule} from '@angular/material/icon';
import {MyAttendanceDayDto, MyAttendanceDayModuleDto} from '../../../../api';
import {DatePipe} from '@angular/common';

@Component({
  selector: 'app-daily-attendance',
  imports: [
    Card,
    MatIconModule,
    DatePipe
  ],
  templateUrl: './daily-attendance.html',
  styleUrl: './daily-attendance.scss'
})
export class DailyAttendance {
  $days = input.required<MyAttendanceDayDto[]>();

  isModuleAttended(module: MyAttendanceDayModuleDto): boolean {
    return module.isAttended === true || module.checkedInAtUtc != null;
  }

  formatTime(time?: string | null): string {
    if (!time) return '';

    // expected: "HH:mm:ss" or "HH:mm"
    const parts = time.split(':');
    if (parts.length < 2) return time;

    const hh = Number(parts[0]);
    const mm = parts[1];
    if (Number.isNaN(hh)) return time;

    const hour12 = ((hh + 11) % 12) + 1;
    const ampm = hh >= 12 ? 'PM' : 'AM';
    return `${hour12}:${mm} ${ampm}`;
  }
}
