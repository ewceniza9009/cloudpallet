import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, AfterViewInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MobileService } from '../../../core/services/mobile.service';

@Component({
  selector: 'app-scan-hub',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule],
  template: `
    <div class="scan-hub-wrapper">
      <input #scannerInput type="text" class="hidden-scanner" (keyup.enter)="onScan($event)" autofocus>

      <div class="scan-hub-card" (click)="focusScanner()">
        <div class="status-indicator" [class.success]="isSuccess()">
           <div class="glow-ring"></div>
           <mat-icon class="status-icon pulse">{{ isSuccess() ? 'done_all' : icon }}</mat-icon>
        </div>

        <div class="text-content">
          <h2>{{ title }}</h2>
          <p>{{ description }}</p>
        </div>
        
        <div class="actions-stack">
          <button mat-flat-button color="accent" class="camera-primary-btn" (click)="onCameraScan($event)">
              <mat-icon>photo_camera</mat-icon> OPEN CAMERA SCANNER
          </button>
          
          <button mat-button class="manual-toggle-btn" (click)="toggleManual($event)">
              <mat-icon>{{ showManual() ? 'close' : 'keyboard' }}</mat-icon>
              {{ showManual() ? 'CANCEL MANUAL ENTRY' : 'USE KEYBOARD ENTRY' }}
          </button>
        </div>

        <div class="manual-input-area" *ngIf="showManual()" (click)="$event.stopPropagation()">
           <mat-form-field appearance="outline" class="full-width premium-field">
              <mat-label>{{ placeholder }}</mat-label>
              <input matInput [(ngModel)]="manualVal" (keyup.enter)="onManualSubmit()" [placeholder]="placeholder">
              <button mat-icon-button matSuffix (click)="onManualSubmit()" [disabled]="!manualVal">
                  <mat-icon>send</mat-icon>
              </button>
           </mat-form-field>
        </div>
      </div>

      <div class="camera-disclaimer" *ngIf="toastMsg()">
          <mat-icon>warning</mat-icon>
          <span>{{ toastMsg() }}</span>
      </div>
    </div>
  `,
  styles: [`
    .scan-hub-wrapper { width: 100%; position: relative; }
    .hidden-scanner { position: absolute; opacity: 0; left: -9999px; pointer-events: none; }

    .scan-hub-card {
      background: rgba(30, 41, 59, 0.4); backdrop-filter: blur(24px);
      border-radius: 40px; border: 1px solid rgba(255,255,255,0.12);
      padding: 3.5rem 2rem; display: flex; flex-direction: column; align-items: center; text-align: center;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.6); transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
      cursor: pointer;
    }
    .scan-hub-card:active { transform: scale(0.98); background: rgba(30, 41, 59, 0.5); }

    .status-indicator {
      width: 140px; height: 140px; border-radius: 40px; background: rgba(28, 131, 117, 0.08);
      border: 3px solid rgba(28, 131, 117, 0.3); position: relative; display: flex; align-items: center; justify-content: center;
      margin-bottom: 2.5rem; transition: all 0.5s cubic-bezier(0.175, 0.885, 0.32, 1.275);
    }
    .status-indicator.success { background: rgba(16, 185, 129, 0.2); border-color: #10b981; transform: scale(1.1) rotate(5deg); }
    
    .glow-ring { position: absolute; inset: -10px; border-radius: 45px; background: radial-gradient(circle, rgba(28, 131, 117, 0.15), transparent 70%); opacity: 0.5; }
    .status-icon { font-size: 4.5rem; width: 4.5rem; height: 4.5rem; color: var(--mat-sys-primary); }
    .status-indicator.success .status-icon { color: #10b981; }

    .text-content { 
      margin-bottom: 2.5rem; 
      h2 { font-size: 1.75rem; font-weight: 900; color: white !important; margin: 0 0 0.75rem; letter-spacing: -0.01em; }
      p { color: #94a3b8; font-size: 1.05rem; line-height: 1.5; margin: 0; }
    }

    .actions-stack { display: flex; flex-direction: column; gap: 1rem; width: 100%; max-width: 320px; }
    .camera-primary-btn { height: 64px; border-radius: 20px; font-weight: 900; font-size: 1.05rem; letter-spacing: 0.02em; box-shadow: 0 10px 20px rgba(0,0,0,0.3); }
    .manual-toggle-btn { height: 50px; color: #64748b !important; font-weight: 700; letter-spacing: 0.05em; }

    .manual-input-area { margin-top: 2rem; width: 100%; max-width: 320px; animation: slideUp 0.3s ease-out; }
    .full-width { width: 100%; }

    @keyframes slideUp { from { transform: translateY(15px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
    .pulse { animation: pulseAnim 3s infinite; }
    @keyframes pulseAnim { 0%, 100% { transform: scale(1); opacity: 1; } 50% { transform: scale(1.05); opacity: 0.8; } }

    .camera-disclaimer {
        position: fixed; bottom: 2rem; left: 1.5rem; right: 1.5rem; background: #ef4444; color: white;
        padding: 1.25rem; border-radius: 20px; display: flex; align-items: center; gap: 1rem; z-index: 1000;
        font-weight: 800; box-shadow: 0 20px 40px rgba(0,0,0,0.5); animation: zoomIn 0.3s ease;
    }
    @keyframes zoomIn { from { transform: scale(0.9); opacity: 0; } to { transform: scale(1); opacity: 1; } }
  `]
})
export class ScanHubComponent implements AfterViewInit {
  @Input() title: string = 'SCAN BARCODE';
  @Input() description: string = 'Point your device at a barcode to begin.';
  @Input() icon: string = 'qr_code_scanner';
  @Input() placeholder: string = 'Enter Value Manually...';

  @Output() scan = new EventEmitter<string>();

  @ViewChild('scannerInput') scannerInput!: ElementRef<HTMLInputElement>;
  
  private mobile = inject(MobileService);
  
  showManual = signal(false);
  isSuccess = signal(false);
  manualVal = '';
  toastMsg = signal<string | null>(null);

  ngAfterViewInit(): void {
    this.focusScanner();
  }

  focusScanner(): void {
    if (this.scannerInput) {
      this.scannerInput.nativeElement.focus();
    }
  }

  onScan(event: any): void {
    const val = event.target.value.trim();
    if (val) {
      this.handleInput(val);
      event.target.value = '';
    }
  }

  onManualSubmit(): void {
    if (this.manualVal.trim()) {
      this.handleInput(this.manualVal.trim());
      this.manualVal = '';
      this.showManual.set(false);
    }
  }

  private handleInput(val: string): void {
    this.mobile.notifyScan();
    this.isSuccess.set(true);
    this.scan.emit(val);
    setTimeout(() => this.isSuccess.set(false), 1200);
  }

  toggleManual(event: Event): void {
    event.stopPropagation();
    this.showManual.update(v => !v);
  }

  onCameraScan(event: Event): void {
    event.stopPropagation();
    this.mobile.notifyScan();
    this.toastMsg.set('CAMERA COMPONENT MISSING. RUN: npm install @zxing/ngx-scanner');
    setTimeout(() => this.toastMsg.set(null), 5000);
  }
}
