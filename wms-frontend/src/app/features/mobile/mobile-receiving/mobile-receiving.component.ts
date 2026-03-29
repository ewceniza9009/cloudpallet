import { Component, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-mobile-receiving',
  standalone: true,
  imports: [CommonModule, RouterModule, MatStepperModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, FormsModule],
  template: `
    <div class="mobile-container">
      <header class="mobile-header">
        <button mat-icon-button routerLink="/mobile/menu">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <h1>Mobile Receiving</h1>
        <div style="flex: 1"></div>
        <button mat-icon-button (click)="showHelp.set(true)">
          <mat-icon>help_outline</mat-icon>
        </button>
      </header>

      <div class="stepper-wrap">
        <mat-stepper orientation="vertical" #stepper>
          <mat-step [completed]="!!poNumber()">
            <ng-template matStepLabel>Select / Scan Document</ng-template>
            <div class="step-content">
               <div class="scan-hub mini" (click)="focusInput(poInput)">
                  <mat-icon class="pulse-icon">description</mat-icon>
                  <button mat-flat-button color="accent" class="camera-btn" (click)="startCameraScan($event)">
                      <mat-icon>photo_camera</mat-icon> SCAN MANIFEST
                  </button>
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Manifest / PO #</mat-label>
                    <input #poInput matInput [(ngModel)]="poNumber" placeholder="PO or ASN #" (keyup.enter)="stepper.next()">
                    <mat-icon matSuffix>qr_code_scanner</mat-icon>
                  </mat-form-field>
               </div>

               <div class="pending-list-section">
                   <div class="section-header">
                       <mat-icon>local_shipping</mat-icon>
                       <span>EXPECTED TODAY</span>
                   </div>
                   <div class="shipment-card animate-pop" *ngFor="let s of pendingShipments()" (click)="selectShipment(s)">
                       <div class="shipment-info">
                           <span class="supplier">{{ s.supplier }}</span>
                           <span class="ref">REF: {{ s.id }}</span>
                       </div>
                       <div class="shipment-meta">
                           <span class="count">{{ s.pallets }} PLT</span>
                           <mat-icon>chevron_right</mat-icon>
                       </div>
                   </div>
               </div>
               
               <p class="hint">Select an arrival or scan the barcode to start.</p>
            </div>
          </mat-step>

          <mat-step [completed]="palletsScanned() > 0">
            <ng-template matStepLabel>Register Pallets (LPN)</ng-template>
            <div class="step-content">
               <div class="intake-hub" (click)="focusInput(lpnInput)">
                  <div class="creation-actions">
                      <button mat-flat-button color="accent" class="big-action-btn" (click)="generateLPN()">
                          <mat-icon>add_box</mat-icon> GENERATE NEW LPN
                      </button>
                      <button mat-stroked-button color="primary" class="big-action-btn" (click)="startCameraScan($event)">
                          <mat-icon>photo_camera</mat-icon> SCAN EXISTING
                      </button>
                  </div>

                  <div class="divider"><span>OR MANUAL ENTRY</span></div>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Pallet LPN #</mat-label>
                    <input #lpnInput matInput [(ngModel)]="currentLPN" placeholder="LPN Barcode">
                    <mat-icon matSuffix>pallet</mat-icon>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Actual Weight (KG)</mat-label>
                    <input matInput type="number" [(ngModel)]="currentWeight" placeholder="0.00">
                    <mat-icon matSuffix>scale</mat-icon>
                  </mat-form-field>

                  <button mat-flat-button color="primary" class="full-width confirm-intake-btn" 
                          [disabled]="!currentLPN()" (click)="addPallet()">
                      <mat-icon>save_alt</mat-icon> REGISTER PALLET
                  </button>
               </div>

               <div class="badge-row" *ngIf="palletsScanned() > 0">
                  <span class="count-badge animate-pop">{{ palletsScanned() }} Pallets Registered</span>
               </div>
               <button mat-button class="full-width next-btn" (click)="stepper.next()">CONTINUE TO SUBMIT</button>
            </div>
          </mat-step>

          <mat-step>
            <ng-template matStepLabel>Submit Session</ng-template>
            <div class="step-content final">
                <div class="receipt-card">
                    <div class="summary-line"><span>PO:</span> <strong>{{ poNumber() || '---' }}</strong></div>
                    <div class="summary-line"><span>Total Pallets:</span> <strong>{{ palletsScanned() }}</strong></div>
                </div>
                <button mat-raised-button color="primary" class="big-submit" (click)="finish()">COMPLETE RECEIVING</button>
            </div>
          </mat-step>
        </mat-stepper>
      </div>

      <!-- Workflow Help Overlay -->
      <div class="help-overlay animate-fade-in" *ngIf="showHelp()" (click)="showHelp.set(false)">
          <div class="help-card animate-slide-up" (click)="$event.stopPropagation()">
              <h2>Receiving Workflow</h2>
              <div class="guide-step">
                  <div class="num">1</div>
                  <div class="txt"><strong>Scan PO/ASN:</strong> Scan the manifest to link this session to a purchase order.</div>
              </div>
              <div class="guide-step">
                  <div class="num">2</div>
                  <div class="txt"><strong>Create/Add Pallets:</strong> Use 'Generate LPN' for new pallets, or scan existing ones. <strong>Log the weight</strong> from your floor scale here.</div>
              </div>
              <div class="guide-step">
                  <div class="num">3</div>
                  <div class="txt"><strong>Submit:</strong> Tap 'Register' for each pallet, then 'Complete' to finalize the intake.</div>
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

    .stepper-wrap { flex: 1; overflow-y: auto; padding-top: 1rem; position: relative; }
    
    .scan-hub.mini { background: rgba(30, 41, 59, 0.4); padding: 1.5rem; border-radius: 24px; text-align: center; }
    .pulse-icon { font-size: 3rem; width: 3rem; height: 3rem; color: #f97316; margin-bottom: 1rem; }
    .camera-btn { width: 100%; height: 50px; border-radius: 12px; font-weight: 800; margin-bottom: 1rem; font-size: 1rem; }

    .full-width { width: 100%; }
    .hint { color: #94a3b8; font-size: 0.85rem; margin-top: 1rem; font-style: italic; }
    
    .badge-row { margin-top: 1rem; display: flex; justify-content: center; }
    .count-badge { background: #10b981; color: white; padding: 0.5rem 1rem; border-radius: 20px; font-weight: 800; font-size: 0.9rem; }

    .final { text-align: center; padding-top: 1rem; }
    .receipt-card { background: #1e293b; padding: 1.5rem; border-radius: 20px; margin-bottom: 2rem; border: 1px dashed rgba(255,255,255,0.2); text-align: left; }
    .summary-line { display: flex; justify-content: space-between; margin-bottom: 0.5rem; font-size: 1rem; }
    .big-submit { width: 100%; height: 60px; border-radius: 16px; font-weight: 800; font-size: 1.1rem; }

    .intake-hub { background: rgba(30, 41, 59, 0.4); padding: 1.5rem; border-radius: 24px; }
    .creation-actions { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 2rem; }
    .big-action-btn { height: 60px; border-radius: 16px; font-weight: 800; font-size: 0.9rem; border-width: 2px !important; }
    .confirm-intake-btn { height: 50px; border-radius: 12px; margin-top: 1rem; font-weight: 800; background: #3b82f6 !important; }
    .divider { display: flex; align-items: center; text-align: center; color: #475569; margin-bottom: 2rem; }
    .divider::before, .divider::after { content: ""; flex: 1; border-bottom: 1px solid #334155; }
    .divider::before { margin-right: 1rem; }
    .divider::after { margin-left: 1rem; }
    .divider span { font-size: 0.7rem; font-weight: 800; letter-spacing: 0.1em; }

    .animate-pop { animation: pop 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275); }
    @keyframes pop { from { transform: scale(0.8); opacity: 0; } to { transform: scale(1); opacity: 1; } }

    .camera-missing-toast { position: fixed; bottom: 2rem; left: 1rem; right: 1rem; background: #ef4444; color: white; padding: 1rem; border-radius: 12px; display: flex; align-items: center; gap: 0.75rem; z-index: 1000; font-weight: 700; box-shadow: 0 10px 30px rgba(0,0,0,0.5); }

    ::ng-deep {
        .mat-stepper-vertical { background: transparent !important; position: relative !important; padding: 0 !important; }
        .mat-stepper-vertical::before { content: ''; position: absolute; top: 2rem; bottom: 2rem; left: 31px; width: 2px; background: rgba(255,255,255,0.1); z-index: 1; }
        .mat-step-header { padding: 1.5rem 1rem !important; overflow: visible !important; display: flex !important; align-items: center; z-index: 2 !important; }
        .mat-step-header .mat-step-icon { width: 32px !important; height: 32px !important; margin-right: 12px !important; margin-left: 0 !important; flex-shrink: 0 !important; z-index: 3 !important; }
        .mat-step-header .mat-step-label { padding-left: 24px !important; font-weight: 800 !important; color: white !important; font-size: 1.15rem !important; }
        .mat-step-content { margin-left: 16px !important; padding: 0 0 1.5rem 2rem !important; border-left: none !important; }
        .mat-stepper-vertical-line::before { display: none !important; }
        .mat-stepper-vertical-line::after { display: none !important; }
    }

    .pending-list-section { margin-top: 2.5rem; padding-bottom: 2rem; }
    .section-header { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.25rem; color: #94a3b8; font-size: 0.75rem; font-weight: 800; letter-spacing: 0.15em; justify-content: flex-start; }
    .section-header mat-icon { font-size: 1.1rem; width: 1.1rem; height: 1.1rem; color: #34d399; }

    .shipment-card {
        background: rgba(30, 41, 59, 0.4); padding: 1.5rem; border-radius: 20px; display: flex; justify-content: space-between; align-items: center;
        margin-bottom: 1rem; border: 1px solid rgba(255,255,255,0.05); cursor: pointer; transition: all 0.2s ease; text-align: left;
    }
    .shipment-card:active { transform: scale(0.97); background: rgba(51, 65, 85, 0.6); border-color: #3b82f6; }
    
    .shipment-info { display: flex; flex-direction: column; gap: 0.5rem; align-items: flex-start; }
    .supplier { color: white; font-weight: 900; font-size: 1rem; letter-spacing: -0.01em; }
    .ref { color: #f97316; font-size: 0.8rem; font-weight: 800; font-family: monospace; opacity: 0.8; }
    
    .shipment-meta { display: flex; align-items: center; gap: 1rem; color: #34d399; font-weight: 900; font-size: 0.9rem; }
    .shipment-meta mat-icon { font-size: 1.5rem; color: #334155; }

    .help-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(10px); z-index: 3000; display: flex; align-items: flex-end; padding: 1.5rem; }
    .help-card { background: #1e293b; width: 100%; padding: 2.5rem; border-radius: 40px 40px 0 0; border-top: 1px solid rgba(255,255,255,0.1); }
    .help-card h2 { font-size: 1.5rem; font-weight: 900; color: #f97316 !important; margin-bottom: 2rem; border-bottom: none; }
    
    .guide-step { display: flex; gap: 1.5rem; margin-bottom: 1.5rem; align-items: flex-start; text-align: left; }
    .guide-step .num { width: 32px; height: 32px; border-radius: 50%; background: #f97316; color: white; display: flex; align-items: center; justify-content: center; font-weight: 900; flex-shrink: 0; }
    .guide-step .txt { font-size: 0.95rem; color: #cbd5e1; line-height: 1.4; }
    .guide-step .txt strong { color: white; display: block; margin-bottom: 2px; }

    .animate-fade-in { animation: fadeIn 0.3s ease-out; }
    .animate-slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
    @keyframes slideUp { from { transform: translateY(40px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
  `]
})
export class MobileReceivingComponent {
  poNumber = signal<string>('');
  palletsScanned = signal<number>(0);
  showHelp = signal(false);
  toastMsg = signal<string | null>(null);

  currentLPN = signal<string>('');
  currentWeight = signal<number | null>(null);

  pendingShipments = signal<any[]>([
    { id: 'ASN-88291', supplier: 'NORTH STAR LOGISTICS', pallets: 12 },
    { id: 'ASN-99102', supplier: 'OMEGA MANUFACTURING', pallets: 8 },
    { id: 'ASN-77210', supplier: 'VELOCITY WHOLSALE', pallets: 15 }
  ]);

  focusInput(el: any) { el.focus(); }

  selectShipment(s: any) {
    this.poNumber.set(s.id);
    // Auto advance to next step
    setTimeout(() => {
        const stepper = (document.querySelector('mat-stepper') as any);
        if(stepper) stepper.next();
    }, 100);
  }

  startCameraScan(event: any) {
    event.stopPropagation();
    this.toastMsg.set('CAMERA SCANNER MISSING. PLEASE RUN npm install @zxing/ngx-scanner');
    setTimeout(() => this.toastMsg.set(null), 5000);
  }

  generateLPN() {
    const newLPN = 'LPN-' + Math.floor(100000 + Math.random() * 900000);
    this.currentLPN.set(newLPN);
  }

  addPallet() {
    if (this.currentLPN()) {
      this.palletsScanned.update(n => n + 1);
      this.currentLPN.set('');
      this.currentWeight.set(null);
    }
  }

  finish() {
    alert('Session Submitted: ' + this.poNumber());
  }
}
