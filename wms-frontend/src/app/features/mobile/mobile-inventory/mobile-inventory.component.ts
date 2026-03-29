import { Component, signal, ViewChild, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { ScanHubComponent } from '../../../shared/components/scan-hub/scan-hub.component';
import { MobileHeaderComponent } from '../../../shared/components/mobile-header/mobile-header.component';

@Component({
  selector: 'app-mobile-inventory',
  standalone: true,
  imports: [
    CommonModule, RouterModule, MatCardModule, MatButtonModule, 
    MatIconModule, FormsModule, ScanHubComponent, MobileHeaderComponent
  ],
  template: `
    <div class="mobile-container">
      <app-mobile-header 
        title="Inventory Ledger" 
        [subtitle]="lookupHistory().length + ' Lookups This Session'"
        [showHelp]="true"
        (help)="showHelp.set(true)">
      </app-mobile-header>

      <div class="content-body">
        <app-scan-hub
          title="ITEM LOOKUP"
          description="Scan a SKU or Location barcode to check its live stock ledger."
          icon="inventory"
          placeholder="Enter SKU, LPN or Location..."
          (scan)="lookupItem($event)">
        </app-scan-hub>

        <div class="history-section" *ngIf="lookupHistory().length > 0">
            <div class="section-label">RECENT LOOKUPS</div>
            <div class="history-list">
                <div class="history-item slide-up" *ngFor="let entry of lookupHistory()">
                    <div class="icon-wrap"><mat-icon>search</mat-icon></div>
                    <div class="history-info">
                        <span class="lpn">{{ entry.id }}</span>
                        <span class="loc">Result: <strong>STOCK LEDGER FOUND</strong></span>
                    </div>
                    <span class="time">{{ entry.time }}</span>
                </div>
            </div>
        </div>
      </div>
      
      <!-- Workflow Help Overlay -->
      <div class="help-overlay animate-fade-in" *ngIf="showHelp()" (click)="showHelp.set(false)">
          <div class="help-card animate-slide-up" (click)="$event.stopPropagation()">
              <h2>Inventory Workflow</h2>
              <div class="guide-step">
                  <div class="num">1</div>
                  <div class="txt"><strong>Scan SKU/LOC/LPN:</strong> Scan any barcode to query its live stock level.</div>
              </div>
              <div class="guide-step">
                  <div class="num">2</div>
                  <div class="txt"><strong>View Ledger:</strong> The system opens the full transaction history for that entity.</div>
              </div>
              <div class="guide-step">
                  <div class="num">3</div>
                  <div class="txt"><strong>Audit:</strong> Verify if the physical stock matches the digital record.</div>
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
    .section-label { font-size: 0.75rem; font-weight: 800; color: #64748b; letter-spacing: 0.15em; margin-bottom: 1rem; }

    .history-item { 
       background: rgba(30, 41, 59, 0.4); padding: 1.25rem; border-radius: 24px; display: flex; align-items: center; gap: 1.25rem; margin-bottom: 0.75rem; 
       border: 1px solid rgba(255,255,255,0.05);
       .icon-wrap { width: 44px; height: 44px; border-radius: 12px; background: rgba(168, 85, 247, 0.1); color: #a855f7; display: flex; align-items: center; justify-content: center; }
    }
    
    .history-info { flex: 1; display: flex; flex-direction: column; .lpn { font-weight: 900; color: white; font-size: 1.1rem; } .loc { font-size: 0.8rem; color: #94a3b8; } }
    .time { font-size: 0.75rem; color: #475569; font-weight: 700; }

    .slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

    .help-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(10px); z-index: 3000; display: flex; align-items: flex-end; padding: 1.5rem; }
    .help-card { background: #1e293b; width: 100%; padding: 2.5rem; border-radius: 40px 40px 0 0; border-top: 1px solid rgba(255,255,255,0.1); }
    .help-card h2 { font-size: 1.5rem; font-weight: 900; color: #a855f7 !important; margin-bottom: 2rem; border-bottom: none; }
    
    .guide-step { display: flex; gap: 1.5rem; margin-bottom: 1.5rem; align-items: flex-start; text-align: left; }
    .guide-step .num { width: 32px; height: 32px; border-radius: 50%; background: #a855f7; color: white; display: flex; align-items: center; justify-content: center; font-weight: 900; flex-shrink: 0; }
    .guide-step .txt { font-size: 0.95rem; color: #cbd5e1; line-height: 1.4; }
    .guide-step .txt strong { color: white; display: block; margin-bottom: 2px; }

    .animate-fade-in { animation: fadeIn 0.3s ease-out; }
    .animate-slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
  `]
})
export class MobileInventoryComponent {
  lookupHistory = signal<any[]>([]);
  showHelp = signal(false);

  lookupItem(id: string) {
    this.lookupHistory.update(h => [{ id: id.toUpperCase(), time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }, ...h]);
  }
}
