import { Component, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-mobile-shipping',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule, FormsModule],
  template: `
    <div class="mobile-container">
      <header class="mobile-header">
        <button mat-icon-button routerLink="/mobile/menu">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <h1>Shipping / Dispatch</h1>
        <div style="flex: 1"></div>
        <button mat-icon-button (click)="showHelp.set(true)">
          <mat-icon>help_outline</mat-icon>
        </button>
      </header>

      <div class="content-body">
        <input #scannerInput type="text" class="hidden-scanner" (keyup.enter)="onScan($event)" autofocus>

        <div class="scan-hub" (click)="focusScanner()">
          <div class="status-circle" [class.success]="isConfirmed()">
             <mat-icon class="pulse-icon">{{ isConfirmed() ? 'check_circle' : 'local_shipping' }}</mat-icon>
          </div>
          <h2>DISPATCH SHIPMENT</h2>
          <p>Scan the carrier tracking label or BOL to confirm shipping.</p>
          
          <div class="scan-actions">
            <button mat-flat-button color="accent" class="big-camera-btn" (click)="startCameraScan($event)">
                <mat-icon>photo_camera</mat-icon> SCAN TRACKING #
            </button>
            <button mat-button class="manual-btn" (click)="showManual.set(!showManual()); $event.stopPropagation()">
                <mat-icon>keyboard</mat-icon> MANUAL ENTRY
            </button>
          </div>

          <div class="manual-form slide-in" *ngIf="showManual()">
             <mat-form-field appearance="outline" class="full-width">
                <input matInput [(ngModel)]="manualInput" (keyup.enter)="onManualSubmit()" placeholder="Enter Tracking #...">
                <button mat-icon-button matSuffix (click)="onManualSubmit()">
                    <mat-icon>send</mat-icon>
                </button>
             </mat-form-field>
          </div>
        </div>

        <div class="history-list" *ngFor="let entry of confirmedList()">
            <div class="history-item slide-up">
                <mat-icon>local_shipping</mat-icon>
                <div class="history-info">
                    <span class="lpn">#{{ entry.id }}</span>
                    <span class="loc">STATUS: <strong>DISPATCHED</strong></span>
                </div>
                <span class="time">{{ entry.time }}</span>
            </div>
        </div>
      </div>
      
      <!-- Workflow Help Overlay -->
      <div class="help-overlay animate-fade-in" *ngIf="showHelp()" (click)="showHelp.set(false)">
          <div class="help-card animate-slide-up" (click)="$event.stopPropagation()">
              <h2>Shipping Workflow</h2>
              <div class="guide-step">
                  <div class="num">1</div>
                  <div class="txt"><strong>Scan Tracking #:</strong> Scan the carrier label on the final packed parcel.</div>
              </div>
              <div class="guide-step">
                  <div class="num">2</div>
                  <div class="txt"><strong>Verify Order:</strong> The system confirms the tracking matches a planned shipment.</div>
              </div>
              <div class="guide-step">
                  <div class="num">3</div>
                  <div class="txt"><strong>Confirm Dispatch:</strong> Order status updates to 'Shipped' in real-time!</div>
              </div>
              <button mat-raised-button color="primary" class="full-width" (click)="showHelp.set(false)">UNDERSTOOD</button>
          </div>
      </div>

      <div class="camera-missing-toast" *ngIf="toastMsg()">
          <mat-icon>warning</mat-icon>
          <span>{{ toastMsg() }}</span>
      </div>
    </div>
  `,
  styles: [`
    .mobile-container { height: 100vh; background: #0f172a; display: flex; flex-direction: column; color: white; overflow: hidden; }
    .mobile-header { padding: 1.25rem 1rem; background: #1e293b; color: white; display: flex; align-items: center; gap: 1rem; border-bottom: 1px solid rgba(255,255,255,0.1); }
    .mobile-header h1 { margin: 0; font-size: 1.15rem; font-weight: 700; color: white !important; }
    .content-body { flex: 1; overflow-y: auto; padding: 2rem 1.5rem; }
    .hidden-scanner { position: absolute; opacity: 0; left: -999px; }

    .scan-hub {
      display: flex; flex-direction: column; align-items: center; text-align: center;
      padding: 3rem 1.5rem; background: rgba(30, 41, 59, 0.7); backdrop-filter: blur(20px);
      border-radius: 40px; border: 1px solid rgba(255,255,255,0.1); margin-bottom: 2rem;
      box-shadow: 0 20px 40px rgba(0,0,0,0.5);
    }
    .status-circle {
      width: 150px; height: 150px; border-radius: 50%; background: rgba(59, 130, 246, 0.15);
      border: 4px solid #3b82f6; display: flex; align-items: center; justify-content: center;
      margin-bottom: 2rem; transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
    }
    .status-circle.success { background: rgba(16, 185, 129, 0.15); border-color: #10b981; transform: scale(1.1); }
    .pulse-icon { font-size: 4.5rem; width: 4.5rem; height: 4.5rem; color: #3b82f6; }
    h2 { font-size: 1.6rem; font-weight: 900; margin-bottom: 0.75rem; color: white !important; }
    p { color: #94a3b8; font-size: 1rem; line-height: 1.5; margin-bottom: 2rem; }
    .scan-actions { display: flex; flex-direction: column; gap: 1rem; width: 100%; }
    .big-camera-btn { height: 60px; border-radius: 20px; font-weight: 800; font-size: 1.1rem; }
    .manual-btn { color: #94a3b8 !important; }
    .manual-form { margin-top: 2rem; width: 100%; }
    .full-width { width: 100%; }
    .history-item { background: #1e293b; padding: 1.25rem; border-radius: 20px; display: flex; align-items: center; gap: 1.25rem; margin-bottom: 1rem; border: 1px solid rgba(255,255,255,0.05); }
    .history-item mat-icon { color: #3b82f6; }
    .camera-missing-toast { position: fixed; bottom: 2rem; left: 1rem; right: 1rem; background: #ef4444; color: white; padding: 1rem; border-radius: 12px; display: flex; align-items: center; gap: 0.75rem; z-index: 1000; font-weight: 700; }
    .slide-in { animation: slideIn 0.3s ease-out; }
    .slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
    @keyframes slideUp { from { transform: translateY(40px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

    .help-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(10px); z-index: 3000; display: flex; align-items: flex-end; padding: 1.5rem; }
    .help-card { background: #1e293b; width: 100%; padding: 2.5rem; border-radius: 40px 40px 0 0; border-top: 1px solid rgba(255,255,255,0.1); }
    .help-card h2 { font-size: 1.5rem; font-weight: 900; color: #3b82f6 !important; margin-bottom: 2rem; border-bottom: none; }
    
    .guide-step { display: flex; gap: 1.5rem; margin-bottom: 1.5rem; align-items: flex-start; text-align: left; }
    .guide-step .num { width: 32px; height: 32px; border-radius: 50%; background: #3b82f6; color: white; display: flex; align-items: center; justify-content: center; font-weight: 900; flex-shrink: 0; }
    .guide-step .txt { font-size: 0.95rem; color: #cbd5e1; line-height: 1.4; }
    .guide-step .txt strong { color: white; display: block; margin-bottom: 2px; }

    .animate-fade-in { animation: fadeIn 0.3s ease-out; }
    .animate-slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
  `]
})
export class MobileShippingComponent implements AfterViewInit {
  @ViewChild('scannerInput') scannerInput!: ElementRef;
  isConfirmed = signal(false);
  confirmedList = signal<any[]>([]);
  showManual = signal(false);
  showHelp = signal(false);
  manualInput = '';
  toastMsg = signal<string | null>(null);
  ngAfterViewInit() { this.focusScanner(); }
  focusScanner() { this.scannerInput.nativeElement.focus(); }
  startCameraScan(event: any) { 
    event.stopPropagation();
    this.toastMsg.set('CAMERA SCANNER COMPONENT MISSING. RUN: npm install @zxing/ngx-scanner');
    setTimeout(() => this.toastMsg.set(null), 5000);
  }
  onScan(event: any) { this.confirmShipment(event.target.value); event.target.value = ''; }
  onManualSubmit() { if(this.manualInput) { this.confirmShipment(this.manualInput); this.manualInput = ''; this.showManual.set(false); } }
  confirmShipment(id: string) {
    this.confirmedList.update(h => [{ id: id.toUpperCase(), time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }, ...h]);
    this.isConfirmed.set(true); setTimeout(() => this.isConfirmed.set(false), 1500);
  }
}
