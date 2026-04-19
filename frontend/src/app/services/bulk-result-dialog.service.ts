import {ApplicationRef, ComponentRef, createComponent, EnvironmentInjector, inject, Injectable} from '@angular/core';
import {BulkResultDialogComponent} from '../components/bulk-result-dialog/bulk-result-dialog.component';
import {BulkResultItem} from '../components/bulk-result-dialog/bulk-result-dialog.models';

@Injectable({
  providedIn: 'root'
})
export class BulkResultDialogService {
  private readonly appRef = inject(ApplicationRef);
  private readonly injector = inject(EnvironmentInjector);

  private dialogRef: ComponentRef<BulkResultDialogComponent> | null = null;

  show(items: BulkResultItem[]): void {
    const dialogRef = createComponent(BulkResultDialogComponent, {
      environmentInjector: this.injector
    });

    this.dialogRef = dialogRef;
    dialogRef.instance.items.set(items);

    dialogRef.instance.onClose.subscribe(() => this.destroy());

    this.appRef.attachView(dialogRef.hostView);
    document.body.appendChild(dialogRef.location.nativeElement);
    dialogRef.changeDetectorRef.detectChanges();
  }

  private destroy(): void {
    if (this.dialogRef) {
      this.appRef.detachView(this.dialogRef.hostView);
      this.dialogRef.location.nativeElement.remove();
      this.dialogRef.destroy();
      this.dialogRef = null;
    }
  }
}
