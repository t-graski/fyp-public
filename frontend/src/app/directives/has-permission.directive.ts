import {Directive, Input, TemplateRef, ViewContainerRef, OnInit, inject} from '@angular/core';
import {PermissionService} from '../services/permission.service';

/**
 * Structural directive to show/hide elements based on user permissions
 *
 * Usage:
 * - Single permission: *appHasPermission="'UserWrite'"
 * - Multiple permissions (any): *appHasPermission="['UserWrite', 'UserDelete']"
 * - All permissions required: *appHasPermission="['UserWrite', 'UserDelete']; mode: 'all'"
 */
@Directive({
  selector: '[appHasPermission]',
  standalone: true
})
export class HasPermissionDirective implements OnInit {
  private readonly permissionService = inject(PermissionService);
  private readonly templateRef = inject(TemplateRef<any>);
  private readonly viewContainer = inject(ViewContainerRef);

  private hasView = false;

  @Input() appHasPermission: string | number | (string | number)[] = [];
  @Input() appHasPermissionMode: 'any' | 'all' = 'any';

  ngOnInit(): void {
    this.updateView();
  }

  private updateView(): void {
    const hasPermission = this.checkPermission();

    if (hasPermission && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!hasPermission && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }

  private checkPermission(): boolean {
    if (!this.appHasPermission) return true;

    const permissions = Array.isArray(this.appHasPermission)
      ? this.appHasPermission
      : [this.appHasPermission];

    if (permissions.length === 0) return true;

    if (this.appHasPermissionMode === 'all') {
      return this.permissionService.hasAllPermissions(...permissions);
    } else {
      return this.permissionService.hasAnyPermission(...permissions);
    }
  }
}
