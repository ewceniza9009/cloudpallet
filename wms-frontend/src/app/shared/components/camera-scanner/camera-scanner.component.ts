import { Component, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ZXingScannerModule } from '@zxing/ngx-scanner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BarcodeFormat } from '@zxing/library';

@Component({
  selector: 'app-camera-scanner',
  standalone: true,
  imports: [CommonModule, ZXingScannerModule, MatButtonModule, MatIconModule],
  template: `
    <div class="camera-container">
      <div class="scanner-wrapper">
        <zxing-scanner
          [formats]="allowedFormats"
          (scanSuccess)="onScanSuccess($event)"
          (scanError)="onScanError($event)"
          (permissionResponse)="onPermissionResponse($event)"
          [enable]="scannerEnabled()"
          [tryHarder]="true"
        ></zxing-scanner>
        
        <!-- Scanning Overlay/Reticle -->
         <div class="scanner-overlay">
            <div class="reticle">
                <div class="corner tl"></div>
                <div class="corner tr"></div>
                <div class="corner bl"></div>
                <div class="corner br"></div>
                <div class="scan-line"></div>
            </div>
         </div>
      </div>

      <div class="scanner-controls">
         <button mat-fab color="warn" (click)="close()">
            <mat-icon>close</mat-icon>
         </button>
      </div>
      
      <div class="status-overlay" *ngIf="errorMessage()">
         <mat-icon>error</mat-icon>
         <span>{{ errorMessage() }}</span>
      </div>
    </div>
  `,
  styles: [`
    .camera-container {
      position: fixed; top: 0; left: 0; width: 100vw; height: 100vh;
      background: #000; z-index: 9999; display: flex; flex-direction: column;
    }
    .scanner-wrapper {
      flex: 1; position: relative; overflow: hidden;
      display: flex; align-items: center; justify-content: center;
    }
    zxing-scanner {
      width: 100%; height: 100%; object-fit: cover;
    }
    .scanner-overlay {
      position: absolute; inset: 0;
      display: flex; align-items: center; justify-content: center;
      background: rgba(0,0,0,0.3); pointer-events: none;
    }
    .reticle {
      width: 260px; height: 260px; position: relative;
    }
    .corner {
      position: absolute; width: 30px; height: 30px; border-color: #10b981; border-style: solid; border-width: 0;
    }
    .tl { top: 0; left: 0; border-top-width: 4px; border-left-width: 4px; border-radius: 8px 0 0 0; }
    .tr { top: 0; right: 0; border-top-width: 4px; border-right-width: 4px; border-radius: 0 8px 0 0; }
    .bl { bottom: 0; left: 0; border-bottom-width: 4px; border-left-width: 4px; border-radius: 0 0 0 8px; }
    .br { bottom: 0; right: 0; border-bottom-width: 4px; border-right-width: 4px; border-radius: 0 0 8px 0; }

    .scan-line {
      position: absolute; left: 10%; right: 10%; top: 50%; height: 2px;
      background: #10b981; box-shadow: 0 0 15px #10b981;
      animation: scanMove 2s infinite ease-in-out;
    }
    @keyframes scanMove { 0%, 100% { top: 10%; } 50% { top: 90%; } }

    .scanner-controls {
      padding: 2rem; display: flex; justify-content: center; background: rgba(0,0,0,0.8);
    }
    .status-overlay {
       position: absolute; top: 2rem; left: 2rem; right: 2rem; 
       background: rgba(239, 68, 68, 0.9); color: white; padding: 1rem;
       border-radius: 12px; display: flex; align-items: center; gap: 0.5rem;
    }
  `]
})
export class CameraScannerComponent {
  @Output() scan = new EventEmitter<string>();
  @Output() closed = new EventEmitter<void>();

  allowedFormats = [
    BarcodeFormat.QR_CODE, 
    BarcodeFormat.CODE_128, 
    BarcodeFormat.CODE_39, 
    BarcodeFormat.EAN_13, 
    BarcodeFormat.EAN_8, 
    BarcodeFormat.UPC_A, 
    BarcodeFormat.UPC_E,
    BarcodeFormat.DATA_MATRIX
  ];
  
  scannerEnabled = signal(true);
  errorMessage = signal<string | null>(null);

  onScanSuccess(result: string): void {
    if (result) {
      this.scan.emit(result);
    }
  }

  onScanError(error: Error): void {
    console.error('Scan error:', error);
    this.errorMessage.set('Error accessing camera: ' + error.message);
  }

  onPermissionResponse(permission: boolean): void {
    if (!permission) {
      this.errorMessage.set('Camera permission denied.');
    }
  }

  close(): void {
    this.closed.emit();
  }
}
