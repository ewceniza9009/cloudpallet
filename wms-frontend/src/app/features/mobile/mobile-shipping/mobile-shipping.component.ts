import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { MobileService } from '../../../core/services/mobile.service';
import { MobileHeaderComponent } from '../../../shared/components/mobile-header/mobile-header.component';
import { ScanHubComponent } from '../../../shared/components/scan-hub/scan-hub.component';

@Component({
  selector: 'app-mobile-shipping',
  standalone: true,
  imports: [
    CommonModule, RouterModule, MatCardModule, MatButtonModule, 
    MatIconModule, FormsModule, MobileHeaderComponent, ScanHubComponent
  ],
  template: `
    <div class="mobile-container">
      <app-mobile-header 
        title="Shipping / Dispatch" 
        [subtitle]="confirmedList().length + ' Orders Dispatched'"
        [showHelp]="true"
        (help)="showHelp.set(true)">
      </app-mobile-header>

      <div class="content-body">
        <app-scan-hub
          title="DISPATCH SHIPMENT"
          description="Scan the carrier tracking label or BOL to confirm shipping."
          icon="local_shipping"
          placeholder="Enter Tracking #..."
          (scan)="confirmShipment($event)">
        </app-scan-hub>

        <div class="history-section" *ngIf="confirmedList().length > 0">
            <div class="section-label">RECENT DISPATCHES</div>
            <div class="history-list">
                <div class="history-item slide-up" *ngFor="let entry of confirmedList()">
                    <div class="icon-wrap info"><mat-icon>local_shipping</mat-icon></div>
                    <div class="history-info">
                        <span class="lpn">#{{ entry.id }}</span>
                        <span class="loc">STATUS: <strong>DISPATCHED</strong></span>
                    </div>
                    <span class="time">{{ entry.time }}</span>
                </div>
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
    </div>
  `,
  styles: [`
    .mobile-container { height: 100vh; background: #020617; display: flex; flex-direction: column; color: white; overflow: hidden; }
    .content-body { flex: 1; overflow-y: auto; padding: 1.5rem; }

    .history-section { margin-top: 2rem; }
    .section-label { font-size: 0.7rem; font-weight: 900; color: #475569; letter-spacing: 0.15em; margin-bottom: 1rem; }

    .history-item { 
       background: rgba(30, 41, 59, 0.4); padding: 1.25rem; border-radius: 24px; display: flex; align-items: center; gap: 1.25rem; margin-bottom: 0.75rem; 
       border: 1px solid rgba(255,255,255,0.05);
       .icon-wrap { width: 44px; height: 44px; border-radius: 12px; background: rgba(59, 130, 246, 0.1); color: #3b82f6; display: flex; align-items: center; justify-content: center; }
    }
    .history-info { flex: 1; display: flex; flex-direction: column; .lpn { font-weight: 900; color: white; font-size: 1.1rem; } .loc { font-size: 0.8rem; color: #94a3b8; } }
    .time { font-size: 0.7rem; color: #475569; font-weight: 700; }

    .help-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(20px); z-index: 3000; display: flex; align-items: flex-end; }
    .help-card { background: #1e293b; width: 100%; padding: 2.5rem; border-radius: 40px 40px 0 0; border-top: 1px solid rgba(255,255,255,0.1); }
    .help-card h2 { font-size: 1.5rem; font-weight: 900; color: #3b82f6 !important; margin-bottom: 2rem; }
    .guide-step { display: flex; gap: 1.5rem; margin-bottom: 1.5rem; align-items: flex-start; }
    .guide-step .num { width: 32px; height: 32px; border-radius: 12px; background: #3b82f6; color: white; display: flex; align-items: center; justify-content: center; font-weight: 900; flex-shrink: 0; }
    .guide-step .txt { font-size: 0.95rem; color: #94a3b8; line-height: 1.5; strong { color: white; } }

    .slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
    .animate-fade-in { animation: fadeIn 0.3s ease-out; }
    .animate-slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
  `]
})
export class MobileShippingComponent {
  private mobile = inject(MobileService);
  confirmedList = signal<any[]>([]);
  showHelp = signal(false);

  confirmShipment(id: string) {
    this.confirmedList.update(h => [{ id: id.toUpperCase(), time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }, ...h]);
    this.mobile.notifySuccess();
  }
}
