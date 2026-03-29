import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CameraScannerComponent } from '../camera-scanner/camera-scanner.component';

@Component({
  selector: 'app-camera-scanner-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, CameraScannerComponent],
  template: `
    <div class="dialog-container">
      <app-camera-scanner (scan)="onScan($event)" (closed)="onClose()"></app-camera-scanner>
    </div>
  `,
  styles: [`
    .dialog-container { width: 100vw; height: 100vh; background: black; }
  `]
})
export class CameraScannerDialogComponent {
  private dialogRef = inject(MatDialogRef<CameraScannerDialogComponent>);

  onScan(result: string): void {
    this.dialogRef.close(result);
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
