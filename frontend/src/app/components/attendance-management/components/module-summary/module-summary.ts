import {Component, input} from '@angular/core';
import {Card} from '../../../card/card';
import {ProgressBar} from '../../../progress-bar/progress-bar';
import {AttendanceColorPipe} from '../../../../shared/pipes/attendance-color-pipe';
import {MyModuleAttendanceSummaryDto} from '../../../../api';
import {DatePipe, DecimalPipe} from '@angular/common';

@Component({
  selector: 'app-module-summary',
  imports: [
    Card,
    ProgressBar,
    AttendanceColorPipe,
    DatePipe,
    DecimalPipe
  ],
  templateUrl: './module-summary.html',
  styleUrl: './module-summary.scss'
})
export class ModuleSummary {
  $modules = input.required<MyModuleAttendanceSummaryDto[]>();

  getDayLabel(day: number): string {
    const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    return days[day] || 'N/A';
  }
}
