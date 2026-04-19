import {ApplicationRef, ComponentRef, createComponent, EnvironmentInjector, inject, Injectable} from '@angular/core';
import {Observable, Subject} from 'rxjs';
import {ConfirmDialogComponent, ConfirmDialogOptions} from '../components/confirm-dialog/confirm-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class ConfirmDialogService {
  private readonly appRef = inject(ApplicationRef);
  private readonly injector = inject(EnvironmentInjector);

  private dialogRef: ComponentRef<ConfirmDialogComponent> | null = null;

  confirm(options: ConfirmDialogOptions = {}): Observable<boolean> {
    const result$ = new Subject<boolean>();

    const dialogRef = createComponent(ConfirmDialogComponent, {
      environmentInjector: this.injector
    });

    this.dialogRef = dialogRef;
    dialogRef.instance.options = options;

    dialogRef.instance.onConfirm.subscribe(() => {
      result$.next(true);
      result$.complete();
      this.destroy();
    });

    dialogRef.instance.onCancel.subscribe(() => {
      result$.next(false);
      result$.complete();
      this.destroy();
    });

    this.appRef.attachView(dialogRef.hostView);
    document.body.appendChild(dialogRef.location.nativeElement);
    dialogRef.changeDetectorRef.detectChanges();

    return result$.asObservable();
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
