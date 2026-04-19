import {Component, input} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {ModuleMemberDto} from '../../../api';

@Component({
  selector: 'app-module-members-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './module-members-tab.component.html',
  styleUrl: './module-members-tab.component.scss'
})
export class ModuleMembersTabComponent {
  $students = input<ModuleMemberDto[]>([]);
  $staff = input<ModuleMemberDto[]>([]);
}
