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
  selector: 'app-mobile-vas',
  standalone: true,
  imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule, FormsModule],
  template: `
    <div class="mobile-container">
      <header class="mobile-header">
        <button mat-icon-button routerLink="/mobile/menu">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <h1>Value Added Services</h1>
      </header>

      <div class="content-body">
        <input #scannerInput type="text" class="hidden-scanner" (keyup.enter)="onScan($event)" autofocus>

        <div class="scan-hub" (click)="focusScanner()">
          <div class="status-circle" [class.success]="lastScan()">
             <mat-icon class="pulse-icon">{{ lastScan() ? 'star' : 'auto_awesome' }}</mat-icon>
          </div>
          <h2>ACTIVATE SERVICE</h2>
          <p>Scan an item to apply Value Added Services (re-labeling, kitting, etc).</p>
          
          <div class="scan-actions">
            <button mat-flat-button color="accent" class="big-camera-btn" (click)="startCameraScan($event)">
                <mat-icon>photo_camera</mat-icon> SCAN ITEM
            </button>
            <button mat-button class="manual-btn" (click)="showManual.set(!showManual()); $event.stopPropagation()">
                <mat-icon>keyboard</mat-icon> MANUAL ENTRY
            </button>
          </div>

          <div class="manual-form slide-in" *ngIf="showManual()">
             <mat-form-field appearance="outline" class="full-width">
                <input matInput [(ngModel)]="manualInput" (keyup.enter)="onManualSubmit()" placeholder="Enter LPN/SKU...">
                <button mat-icon-button matSuffix (click)="onManualSubmit()">
                    <mat-icon>send</mat-icon>
                </button>
             </mat-form-field>
          </div>
        </div>

        <div class="service-grid" *ngIf="lastScan()">
            <mat-card class="service-card animate-pop" (click)="apply('RELABEL')">
                <mat-icon>label</mat-icon>
                <span>RELABEL</span>
            </mat-card>
            <mat-card class="service-card animate-pop" (click)="apply('KIT')">
                <mat-icon>inventory_2</mat-icon>
                <span>KIT / BUNDLE</span>
            </mat-card>
            <mat-card class="service-card animate-pop" (click)="apply('QC')">
                <mat-icon>verified</mat-icon>
                <span>QC INSPECT</span>
            </mat-card>
        </div>

        <div class="history-list" *ngFor="let entry of vasHistory()">
            <div class="history-item slide-up">
                <mat-icon>star</mat-icon>
                <div class="history-info">
                    <span class="lpn">{{ entry.id }}</span>
                    <span class="loc">Service: <strong>{{ entry.service }}</strong></span>
                </div>
                <span class="time">{{ entry.time }}</span>
            </div>
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
      width: 150px; height: 150px; border-radius: 50%; background: rgba(14, 165, 233, 0.15);
      border: 4px solid #0ea5e9; display: flex; align-items: center; justify-content: center;
      margin-bottom: 2rem; transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
    }
    .status-circle.success { background: rgba(16, 185, 129, 0.15); border-color: #10b981; transform: scale(1.1); }
    .pulse-icon { font-size: 4.5rem; width: 4.5rem; height: 4.5rem; color: #0ea5e9; }
    h2 { font-size: 1.6rem; font-weight: 900; margin-bottom: 0.75rem; color: white !important; }
    p { color: #94a3b8; font-size: 1rem; line-height: 1.5; margin-bottom: 2rem; }
    .scan-actions { display: flex; flex-direction: column; gap: 1rem; width: 100%; }
    .big-camera-btn { height: 60px; border-radius: 20px; font-weight: 800; font-size: 1.1rem; }
    .manual-btn { color: #94a3b8 !important; }
    .manual-form { margin-top: 2rem; width: 100%; }
    
    .service-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin-bottom: 2rem; }
    .service-card { background: #1e293b; color: white; padding: 1rem; display: flex; flex-direction: column; align-items: center; gap: 0.5rem; border-radius: 16px; text-align: center; border: 1px solid rgba(255,255,255,0.05); }
    .service-card mat-icon { font-size: 2rem; width: 2rem; height: 2rem; color: #0ea5e9; }
    .service-card span { font-size: 0.75rem; font-weight: 800; }

    .history-item { background: #1e293b; padding: 1.25rem; border-radius: 20px; display: flex; align-items: center; gap: 1.25rem; margin-bottom: 1rem; border-left: 4px solid #0ea5e9; }
    .history-item mat-icon { color: #0ea5e9; }
    .camera-missing-toast { position: fixed; bottom: 2rem; left: 1rem; right: 1rem; background: #ef4444; color: white; padding: 1rem; border-radius: 12px; display: flex; align-items: center; gap: 0.75rem; z-index: 1000; font-weight: 700; }
    .slide-in { animation: slideIn 0.3s ease-out; }
    .slide-up { animation: slideUp 0.4s ease-out; }
    .animate-pop { animation: pop 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275); }
    @keyframes pop { from { transform: scale(0.8); opacity: 0; } to { transform: scale(1); opacity: 1; } }
    @keyframes slideIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
    @keyframes slideUp { from { transform: translateY(40px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
  `]
})
export class MobileVASComponent implements AfterViewInit {
  @ViewChild('scannerInput') scannerInput!: ElementRef;
  lastScan = signal<string | null>(null);
  vasHistory = signal<any[]>([]);
  showManual = signal(false);
  manualInput = '';
  toastMsg = signal<string | null>(null);
  ngAfterViewInit() { this.focusScanner(); }
  focusScanner() { this.scannerInput.nativeElement.focus(); }
  startCameraScan(event: any) { 
    event.stopPropagation();
    this.toastMsg.set('CAMERA COMPONENT MISSING. RUN: npm install @zxing/ngx-scanner');
    setTimeout(() => this.toastMsg.set(null), 5000);
  }
  onScan(event: any) { this.lastScan.set(event.target.value); event.target.value = ''; }
  onManualSubmit() { if(this.manualInput) { this.lastScan.set(this.manualInput); this.manualInput = ''; this.showManual.set(false); } }
  apply(service: string) {
    this.vasHistory.update(h => [{ id: this.lastScan(), service, time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }, ...h]);
    this.lastScan.set(null);
  }
}
