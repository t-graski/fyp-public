import {Component, input} from '@angular/core';
import {Card} from '../../../card/card';
import {ProgressBar} from '../../../progress-bar/progress-bar';
import {AttendanceColorPipe} from '../../../../shared/pipes/attendance-color-pipe';
import {MyAttendanceOverviewDto} from '../../../../api';
import {DatePipe, DecimalPipe} from '@angular/common';

@Component({
  selector: 'app-attendance-overview',
  imports: [
    Card,
    ProgressBar,
    AttendanceColorPipe,
    DatePipe,
    DecimalPipe
  ],
  templateUrl: './attendance-overview.html',
  styleUrl: './attendance-overview.scss'
})
export class AttendanceOverview {
  $overview = input.required<MyAttendanceOverviewDto>();
}
