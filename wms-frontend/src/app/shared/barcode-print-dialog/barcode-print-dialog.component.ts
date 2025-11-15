import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
  MatDialogModule,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

export interface BarcodePrintData {
  title: string;
  barcodeText: string;
  type: 'Item' | 'Pallet' | 'Material';
  quantity?: number;
}

@Component({
  selector: 'app-barcode-print-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <h4 mat-dialog-title class="dialog-title">
      <mat-icon color="primary">print</mat-icon>
      <span>{{ data.title }}</span>
    </h4>
    <mat-dialog-content class="dialog-content">
      <div class="preview-container">
        @if (isLoading()) {
        <div class="spinner-overlay">
          <mat-progress-spinner
            mode="indeterminate"
            diameter="40"
          ></mat-progress-spinner>
          <span>Loading PDF preview...</span>
        </div>
        }
        <iframe
          [src]="pdfUrl"
          frameborder="0"
          width="100%"
          height="180px"
          (load)="isLoading.set(false)"
        ></iframe>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Close</button>
      <button
        mat-flat-button
        color="primary"
        (click)="onPrint()"
        [disabled]="isLoading()"
      >
        <mat-icon>print</mat-icon>
        Print Label
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .dialog-title {
        display: flex;
        align-items: center;
        gap: 8px;
      }
      .dialog-content {
        display: flex;
        flex-direction: column;
        width: 450px;
        gap: 1rem;
        overflow-x: hidden;
      }
      .barcode-text {
        font-family: monospace;
        font-size: 1.1em;
        font-weight: 600;
        padding: 0.5rem;
        background: var(--mat-sys-surface-container);
        border-radius: 4px;
        word-break: break-all;
      }
      .barcode-label {
        margin: 0;
        color: var(--mat-sys-on-surface-variant);
        font-size: 0.875rem;
      }
      .preview-container {
        position: relative;
      }
      .spinner-overlay {
        position: absolute;
        inset: 0;
        background: rgba(255, 255, 255, 0.9);
        z-index: 10;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        gap: 10px;
      }
    `,
  ],
})
export class BarcodePrintDialogComponent {
  private sanitizer = inject(DomSanitizer);
  private authService = inject(AuthService);
  public dialogRef = inject(MatDialogRef<BarcodePrintDialogComponent>);
  public data: BarcodePrintData = inject(MAT_DIALOG_DATA);
  public isLoading = signal(true);

  pdfUrl: SafeUrl;

  constructor() {
    const token = this.authService.getToken();

    let url = `${
      environment.apiUrl
    }/Lookups/barcode-image?barcodeText=${encodeURIComponent(
      this.data.barcodeText
    )}&type=${this.data.type}`;

    if (token) {
      url += `&access_token=${token}`;
    }

    if (this.data.quantity) {
      url += `&quantity=${this.data.quantity}`;
    }

    this.pdfUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  onPrint(): void {
    const iframe = document.querySelector('iframe');
    if (iframe && iframe.contentWindow) {
      iframe.contentWindow.focus();
      iframe.contentWindow.print();
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
