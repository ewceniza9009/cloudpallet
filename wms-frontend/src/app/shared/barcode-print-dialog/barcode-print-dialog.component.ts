import { Component, Inject, inject, signal, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
  MatDialogModule,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DomSanitizer, SafeHtml, SafeResourceUrl, SafeUrl } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { Subscription } from 'rxjs';

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
      <mat-icon>print</mat-icon>
      <span>{{ data.title }}</span>
    </h4>
    <mat-dialog-content class="dialog-content">
      <div class="preview-container">
        @if (isLoading()) {
        <div class="spinner-overlay">
          <mat-progress-spinner
            mode="indeterminate"
            diameter="32"
          ></mat-progress-spinner>
          <span>Generating Label...</span>
        </div>
        }
        
        @if (contentType === 'html' && safeHtmlContent) {
          <div class="html-preview" [innerHTML]="safeHtmlContent"></div>
        } @else if (contentType === 'image' && previewUrl) {
          <img 
            [src]="previewUrl" 
            alt="Barcode Label"
            class="barcode-preview-img"
          />
        } @else if (contentType === 'pdf' && previewUrl) {
           <iframe
            [src]="previewUrl"
            frameborder="0"
            width="100%"
            height="220px"
          ></iframe>
        }

        <!-- Hidden Iframe for Printing -->
        <iframe
          id="print-frame"
          [src]="printUrl"
          style="position: absolute; width: 0; height: 0; border: 0; visibility: hidden;"
        ></iframe>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end" class="dialog-actions">
      <button mat-stroked-button (click)="onCancel()">Close</button>
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
        gap: 12px;
        margin: 0;
        padding: 16px 24px;
        border-bottom: 1px solid var(--mat-sys-outline-variant);
        background-color: var(--mat-sys-surface-container-low);
        
        mat-icon {
          color: var(--mat-sys-primary);
        }
        
        span {
          font-size: 1.25rem;
          font-weight: 500;
        }
      }
      
      .dialog-content {
        display: flex;
        flex-direction: column;
        width: 500px;
        gap: 1.5rem;
        padding: 24px !important;
        overflow-x: hidden;
        align-items: center;
      }

      .preview-container {
        position: relative;
        width: 100%;
        display: flex;
        justify-content: center;
        background-color: var(--mat-sys-surface-container-lowest);
        border: 1px solid var(--mat-sys-outline-variant);
        border-radius: 8px;
        padding: 1rem;
        box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        min-height: 150px; // Ensure min height for spinner
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
        gap: 12px;
        border-radius: 8px;
        
        span {
          color: var(--mat-sys-on-surface-variant);
          font-size: 0.9rem;
        }
      }
      
      iframe {
        border-radius: 4px;
        background-color: white;
        display: block;
      }
      
      .barcode-preview-img {
        max-width: 100%;
        height: auto;
        display: block;
        border-radius: 4px;
        box-shadow: 0 1px 3px rgba(0,0,0,0.1);
      }

      .html-preview {
        background-color: white;
        padding: 0;
        border-radius: 4px;
        overflow: hidden;
        // Ensure content doesn't overflow
        max-width: 100%;
      }
      
      .dialog-actions {
        padding: 16px 24px;
        border-top: 1px solid var(--mat-sys-outline-variant);
        margin-bottom: 0;
        gap: 8px;
      }
    `,
  ],
})
export class BarcodePrintDialogComponent implements OnInit, OnDestroy {
  private sanitizer = inject(DomSanitizer);
  private authService = inject(AuthService);
  private http = inject(HttpClient);
  public dialogRef = inject(MatDialogRef<BarcodePrintDialogComponent>);
  public data: BarcodePrintData = inject(MAT_DIALOG_DATA);
  public isLoading = signal(true);

  printUrl: SafeResourceUrl | undefined;
  previewUrl: SafeResourceUrl | SafeUrl | undefined;
  safeHtmlContent: SafeHtml | undefined;
  contentType: 'html' | 'pdf' | 'image' | 'unknown' = 'unknown';
  
  private sub: Subscription | undefined;
  private objectUrl: string | undefined;

  ngOnInit() {
    this.loadContent();
  }

  ngOnDestroy() {
    if (this.sub) {
      this.sub.unsubscribe();
    }
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
    }
  }

  loadContent() {
    const token = this.authService.getToken();
    let url = `${environment.apiUrl}/Lookups/barcode-image?barcodeText=${encodeURIComponent(this.data.barcodeText)}&type=${this.data.type}`;

    if (token) {
      url += `&access_token=${token}`;
    }

    if (this.data.quantity) {
      url += `&quantity=${this.data.quantity}`;
    }

    // Set print URL immediately (iframe handles its own loading for print)
    this.printUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);

    // Fetch for preview
    this.sub = this.http.get(url, { responseType: 'blob', observe: 'response' }).subscribe({
      next: (response) => {
        const type = response.headers.get('content-type');
        const blob = response.body;

        if (blob) {
          if (type?.includes('text/html')) {
            this.contentType = 'html';
            blob.text().then(text => {
              this.safeHtmlContent = this.sanitizer.bypassSecurityTrustHtml(text);
              this.isLoading.set(false);
            });
          } else if (type?.includes('application/pdf')) {
            this.contentType = 'pdf';
            this.objectUrl = URL.createObjectURL(blob);
            this.previewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.objectUrl);
            this.isLoading.set(false);
          } else if (type?.includes('image/')) {
            this.contentType = 'image';
            this.objectUrl = URL.createObjectURL(blob);
            this.previewUrl = this.sanitizer.bypassSecurityTrustUrl(this.objectUrl);
            this.isLoading.set(false);
          } else {
            // Fallback to iframe if unknown
            this.contentType = 'pdf'; // Assume PDF/iframe compatible
            this.previewUrl = this.printUrl; // Use original URL
            this.isLoading.set(false);
          }
        } else {
           this.isLoading.set(false);
        }
      },
      error: (err) => {
        console.error('Error loading barcode preview', err);
        this.isLoading.set(false);
        // Fallback to iframe on error, maybe it works there?
        this.contentType = 'pdf';
        this.previewUrl = this.printUrl;
      }
    });
  }

  onPrint(): void {
    const iframe = document.querySelector('#print-frame') as HTMLIFrameElement;
    if (iframe && iframe.contentWindow) {
      iframe.contentWindow.focus();
      iframe.contentWindow.print();
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
