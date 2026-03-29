import { Component, signal, ViewChild, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { MobileService } from '../../../core/services/mobile.service';
import { MobileHeaderComponent } from '../../../shared/components/mobile-header/mobile-header.component';
import { ScanHubComponent } from '../../../shared/components/scan-hub/scan-hub.component';

@Component({
  selector: 'app-mobile-receiving',
  standalone: true,
  imports: [
    CommonModule, RouterModule, MatStepperModule, MatFormFieldModule, 
    MatInputModule, MatButtonModule, MatIconModule, FormsModule,
    MobileHeaderComponent, ScanHubComponent
  ],
  template: `
    <div class="mobile-container">
      <app-mobile-header 
        title="Mobile Receiving" 
        [subtitle]="palletsScanned() + ' Pallets Processed'"
        [showHelp]="true"
        (help)="showHelp.set(true)">
      </app-mobile-header>

      <div class="stepper-wrap">
        <mat-stepper orientation="vertical" #stepper>
          <mat-step [completed]="!!poNumber()">
            <ng-template matStepLabel>Select / Scan Document</ng-template>
            <div class="step-content">
               <app-scan-hub
                 title="MANIFEST SCAN"
                 description="Scan the PO or ASN barcode from your paperwork."
                 icon="description"
                 placeholder="Enter PO/ASN #..."
                 (scan)="onPOScan($event)">
               </app-scan-hub>

               <div class="pending-list-section">
                   <div class="section-header">
                       <mat-icon>local_shipping</mat-icon>
                       <span>EXPECTED ARRIVALS</span>
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
               <div class="intake-hub">
                  <div class="creation-actions">
                      <button mat-flat-button color="accent" class="big-action-btn" (click)="generateLPN()">
                          <mat-icon>add_box</mat-icon> AUTO GENERATE
                      </button>
                      <button mat-stroked-button color="primary" class="big-action-btn" (click)="manualLPNFocus()">
                          <mat-icon>keyboard</mat-icon> MANUAL LPN
                      </button>
                  </div>

                  <div class="divider"><span>LPN REGISTRATION</span></div>

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

               <div class="stats-row" *ngIf="palletsScanned() > 0">
                  <span class="stat-pill animate-pop">{{ palletsScanned() }} Registered</span>
               </div>
               <button mat-button class="full-width next-step-btn" (click)="stepper.next()">CONTINUE TO SUBMIT</button>
            </div>
          </mat-step>

          <mat-step>
            <ng-template matStepLabel>Submit Session</ng-template>
            <div class="step-content final">
                <div class="receipt-card glass-effect">
                    <div class="receipt-header">Verification Summary</div>
                    <div class="summary-line"><span>PO / ASN:</span> <strong>{{ poNumber() || '---' }}</strong></div>
                    <div class="summary-line"><span>Total Units:</span> <strong>{{ palletsScanned() }} Pallets</strong></div>
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
    </div>
  `,
  styles: [`
    .mobile-container { height: 100vh; background: #020617; display: flex; flex-direction: column; color: white; overflow: hidden; }
    .stepper-wrap { flex: 1; overflow-y: auto; padding-top: 1rem; position: relative; }
    
    .scan-hub.mini { background: rgba(30, 41, 59, 0.4); padding: 1.5rem; border-radius: 24px; text-align: center; }
    .pulse-icon { font-size: 3rem; width: 3rem; height: 3rem; color: #f97316; margin-bottom: 1rem; }
    .camera-btn { width: 100%; height: 50px; border-radius: 12px; font-weight: 800; margin-bottom: 1rem; font-size: 1rem; }

    .full-width { width: 100%; }
    .hint { color: #475569; font-size: 0.85rem; margin-top: 1.5rem; font-style: italic; text-align: center; }
    
    .stats-row { margin-top: 1.5rem; display: flex; justify-content: center; }
    .stat-pill { background: rgba(16, 185, 129, 0.1); color: #10b981; padding: 0.6rem 1.25rem; border-radius: 20px; font-weight: 900; font-size: 0.9rem; border: 1px solid rgba(16, 185, 129, 0.2); }

    .final { text-align: center; padding: 1rem; }
    .receipt-card { background: rgba(30, 41, 59, 0.4); padding: 2rem; border-radius: 32px; margin-bottom: 2rem; border: 1px solid rgba(255,255,255,0.1); text-align: left; }
    .receipt-header { font-size: 0.75rem; font-weight: 900; color: var(--mat-sys-primary); letter-spacing: 0.15em; text-transform: uppercase; margin-bottom: 1.5rem; }
    .summary-line { display: flex; justify-content: space-between; margin-bottom: 0.5rem; font-size: 1rem; color: #94a3b8; strong { color: white; } }
    .big-submit { width: 100%; height: 60px; border-radius: 16px; font-weight: 900; font-size: 1.1rem; box-shadow: 0 12px 32px rgba(28, 131, 117, 0.3); }

    .intake-hub { background: rgba(30, 41, 59, 0.4); padding: 1.5rem; border-radius: 32px; border: 1px solid rgba(255,255,255,0.08); }
    .creation-actions { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 2rem; }
    .big-action-btn { height: 60px; border-radius: 16px; font-weight: 800; font-size: 0.85rem; border-width: 1px !important; }
    .confirm-intake-btn { height: 50px; border-radius: 12px; margin-top: 1rem; font-weight: 900; background: var(--mat-sys-primary) !important; }
    .divider { display: flex; align-items: center; text-align: center; color: #475569; margin-bottom: 2rem; }
    .divider::before, .divider::after { content: ""; flex: 1; border-bottom: 1px solid rgba(255,255,255,0.05); }
    .divider::before { margin-right: 1rem; }
    .divider::after { margin-left: 1rem; }
    .divider span { font-size: 0.65rem; font-weight: 800; letter-spacing: 0.1em; }
    
    .next-step-btn { margin-top: 1rem; color: #64748b !important; font-weight: 700; height: 44px; }

    .pending-list-section { margin-top: 2rem; }
    .section-header { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.25rem; color: #64748b; font-size: 0.75rem; font-weight: 900; letter-spacing: 0.15em; }
    .section-header mat-icon { color: #10b981; }

    .shipment-card {
        background: rgba(30, 41, 59, 0.3); padding: 1.5rem; border-radius: 24px; display: flex; justify-content: space-between; align-items: center;
        margin-bottom: 1rem; border: 1px solid rgba(255,255,255,0.05); cursor: pointer; transition: all 0.2s ease;
    }
    .shipment-card:active { transform: scale(0.97); background: rgba(51, 65, 85, 0.4); border-color: #3b82f6; }
    .shipment-info { display: flex; flex-direction: column; gap: 4px; .supplier { font-weight: 900; font-size: 1rem; } .ref { color: #f97316; font-size: 0.8rem; font-family: 'JetBrains Mono', monospace; } }
    .shipment-meta { display: flex; align-items: center; gap: 1rem; color: #10b981; font-weight: 900; }

    .help-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(20px); z-index: 3000; display: flex; align-items: flex-end; }
    .help-card { background: #1e293b; width: 100%; padding: 2.5rem; border-radius: 40px 40px 0 0; border-top: 1px solid rgba(255,255,255,0.1); }
    .help-card h2 { font-size: 1.5rem; font-weight: 900; color: #f97316 !important; margin-bottom: 2rem; }
    .guide-step { display: flex; gap: 1.5rem; margin-bottom: 1.5rem; align-items: flex-start; }
    .guide-step .num { width: 32px; height: 32px; border-radius: 12px; background: #f97316; color: white; display: flex; align-items: center; justify-content: center; font-weight: 900; flex-shrink: 0; }
    .guide-step .txt { font-size: 0.95rem; color: #94a3b8; line-height: 1.5; strong { color: white; } }

    ::ng-deep {
        .mat-stepper-vertical { background: transparent !important; }
        .mat-step-header { padding: 1.5rem 1rem !important; }
        .mat-step-header .mat-step-label { font-weight: 900 !important; color: white !important; font-size: 1.15rem !important; }
        .mat-step-content { margin-left: 20px !important; padding: 0 0 1.5rem 2rem !important; border-left: 2px solid rgba(255,255,255,0.05) !important; }
        .mat-stepper-vertical-line::before { border-left-width: 0 !important; }
    }

    .animate-pop { animation: pop 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275); }
    @keyframes pop { from { transform: scale(0.8); opacity: 0; } to { transform: scale(1); opacity: 1; } }
    .animate-fade-in { animation: fadeIn 0.3s ease-out; }
    .animate-slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
    @keyframes slideUp { from { transform: translateY(40px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
  `]
})
export class MobileReceivingComponent {
  private mobile = inject(MobileService);
  poNumber = signal<string>('');
  palletsScanned = signal<number>(0);
  showHelp = signal(false);

  currentLPN = signal<string>('');
  currentWeight = signal<number | null>(null);

  @ViewChild('lpnInput') lpnInput!: ElementRef;

  pendingShipments = signal<any[]>([
    { id: 'ASN-88291', supplier: 'NORTH STAR LOGISTICS', pallets: 12 },
    { id: 'ASN-99102', supplier: 'OMEGA MANUFACTURING', pallets: 8 },
    { id: 'ASN-77210', supplier: 'VELOCITY WHOLSALE', pallets: 15 }
  ]);

  onPOScan(val: string) {
    this.poNumber.set(val);
    this.advanceStepper();
  }

  selectShipment(s: any) {
    this.poNumber.set(s.id);
    this.mobile.notifyScan();
    this.advanceStepper();
  }

  manualLPNFocus() {
    this.lpnInput.nativeElement.focus();
  }

  generateLPN() {
    const newLPN = 'LPN-' + Math.floor(100000 + Math.random() * 900000);
    this.currentLPN.set(newLPN);
    this.mobile.notifyScan();
  }

  addPallet() {
    if (this.currentLPN()) {
      this.palletsScanned.update(n => n + 1);
      this.currentLPN.set('');
      this.currentWeight.set(null);
      this.mobile.notifySuccess();
    }
  }

  finish() {
    this.mobile.notifySuccess();
    alert('Session Submitted: ' + this.poNumber());
  }

  private advanceStepper() {
    setTimeout(() => {
        const stepper = (document.querySelector('mat-stepper') as any);
        if(stepper) stepper.next();
    }, 100);
  }
}
