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
  selector: 'app-mobile-vas',
  standalone: true,
  imports: [
    CommonModule, RouterModule, MatCardModule, MatButtonModule, 
    MatIconModule, FormsModule, MobileHeaderComponent, ScanHubComponent
  ],
  template: `
    <div class="mobile-container">
      <app-mobile-header 
        title="Value Added Services" 
        [subtitle]="vasHistory().length + ' Services Applied Today'"
        [showHelp]="true"
        (help)="showHelp.set(true)">
      </app-mobile-header>

      <div class="content-body">
        <app-scan-hub
          title="IDENTIFY ITEM"
          description="Scan an item to apply Value Added Services (re-labeling, kitting, etc)."
          icon="auto_awesome"
          placeholder="Enter LPN or SKU..."
          (scan)="onItemIdentified($event)">
        </app-scan-hub>

        <div class="service-selection" *ngIf="activeItem()">
            <div class="selection-header">
                <mat-icon>star</mat-icon>
                <span>SELECT SERVICE FOR {{ activeItem() }}</span>
            </div>
            <div class="service-grid">
                <div class="service-card-premium animate-pop" (click)="apply('RELABEL')">
                    <div class="icon-box relabel"><mat-icon>label</mat-icon></div>
                    <span>RELABEL</span>
                </div>
                <div class="service-card-premium animate-pop" (click)="apply('KIT')">
                    <div class="icon-box kit"><mat-icon>inventory_2</mat-icon></div>
                    <span>KIT / BUNDLE</span>
                </div>
                <div class="service-card-premium animate-pop" (click)="apply('QC')">
                    <div class="icon-box qc"><mat-icon>verified</mat-icon></div>
                    <span>QC INSPECT</span>
                </div>
            </div>
        </div>

        <div class="history-section" *ngIf="vasHistory().length > 0">
            <div class="section-label">RECENT SERVICES</div>
            <div class="history-list">
                <div class="history-item slide-up" *ngFor="let entry of vasHistory()">
                    <div class="icon-wrap info"><mat-icon>auto_awesome</mat-icon></div>
                    <div class="history-info">
                        <span class="lpn">{{ entry.id }}</span>
                        <span class="loc">Service: <strong>{{ entry.service }}</strong></span>
                    </div>
                    <span class="time">{{ entry.time }}</span>
                </div>
            </div>
        </div>
      </div>

       <!-- Workflow Help Overlay -->
       <div class="help-overlay animate-fade-in" *ngIf="showHelp()" (click)="showHelp.set(false)">
          <div class="help-card animate-slide-up" (click)="$event.stopPropagation()">
              <h2>VAS Workflow</h2>
              <div class="guide-step">
                  <div class="num">1</div>
                  <div class="txt"><strong>Identify Entity:</strong> Scan the LPN or SKU that requires a value-added service.</div>
              </div>
              <div class="guide-step">
                  <div class="num">2</div>
                  <div class="txt"><strong>Select Service:</strong> Choose the specialized task (Relabeling, Kitting, QC) from the grid.</div>
              </div>
              <div class="guide-step">
                  <div class="num">3</div>
                  <div class="txt"><strong>Execute:</strong> The system logs the service against the entity for billing and tracking.</div>
              </div>
              <button mat-raised-button color="primary" class="full-width" (click)="showHelp.set(false)">UNDERSTOOD</button>
          </div>
      </div>
    </div>
  `,
  styles: [`
    .mobile-container { height: 100vh; background: #020617; display: flex; flex-direction: column; color: white; overflow: hidden; }
    .content-body { flex: 1; overflow-y: auto; padding: 1.5rem; }

    .service-selection { margin-top: 2rem; }
    .selection-header { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1.25rem; color: #0ea5e9; font-size: 0.7rem; font-weight: 900; letter-spacing: 0.1em; }

    .service-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem; margin-bottom: 2rem; }
    .service-card-premium {
        background: rgba(30, 41, 59, 0.4); padding: 1.5rem 0.5rem; border-radius: 24px; border: 1px solid rgba(255,255,255,0.05);
        display: flex; flex-direction: column; align-items: center; gap: 1rem; text-align: center; cursor: pointer; transition: all 0.2s ease;
        &:active { transform: scale(0.95); background: rgba(51, 65, 85, 0.6); }
        .icon-box {
            width: 50px; height: 50px; border-radius: 16px; display: flex; align-items: center; justify-content: center;
            &.relabel { background: rgba(249, 115, 22, 0.1); color: #f97316; }
            &.kit { background: rgba(14, 165, 233, 0.1); color: #0ea5e9; }
            &.qc { background: rgba(16, 185, 129, 0.1); color: #10b981; }
            mat-icon { font-size: 24px; }
        }
        span { font-size: 0.7rem; font-weight: 900; color: #94a3b8; letter-spacing: 0.05em; }
    }

    .history-section { margin-top: 2rem; }
    .section-label { font-size: 0.7rem; font-weight: 900; color: #475569; letter-spacing: 0.15em; margin-bottom: 1rem; }

    .history-item { 
       background: rgba(30, 41, 59, 0.3); padding: 1.25rem; border-radius: 24px; display: flex; align-items: center; gap: 1.25rem; margin-bottom: 0.75rem; 
       border: 1px solid rgba(255,255,255,0.05);
       .icon-wrap { width: 44px; height: 44px; border-radius: 12px; background: rgba(14, 165, 233, 0.1); color: #0ea5e9; display: flex; align-items: center; justify-content: center; }
    }
    .history-info { flex: 1; display: flex; flex-direction: column; .lpn { font-weight: 900; color: white; font-size: 1.1rem; } .loc { font-size: 0.8rem; color: #94a3b8; } }
    .time { font-size: 0.7rem; color: #475569; font-weight: 700; }

    .help-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(20px); z-index: 3000; display: flex; align-items: flex-end; }
    .help-card { background: #1e293b; width: 100%; padding: 2.5rem; border-radius: 40px 40px 0 0; border-top: 1px solid rgba(255,255,255,0.1); }
    .help-card h2 { font-size: 1.5rem; font-weight: 900; color: #0ea5e9 !important; margin-bottom: 2rem; }
    .guide-step { display: flex; gap: 1.5rem; margin-bottom: 1.5rem; align-items: flex-start; }
    .guide-step .num { width: 32px; height: 32px; border-radius: 12px; background: #0ea5e9; color: white; display: flex; align-items: center; justify-content: center; font-weight: 900; flex-shrink: 0; }
    .guide-step .txt { font-size: 0.95rem; color: #94a3b8; line-height: 1.5; strong { color: white; } }

    .animate-pop { animation: pop 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275); }
    .slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes pop { from { transform: scale(0.8); opacity: 0; } to { transform: scale(1); opacity: 1; } }
    @keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
    .animate-fade-in { animation: fadeIn 0.3s ease-out; }
    .animate-slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
  `]
})
export class MobileVASComponent {
  private mobile = inject(MobileService);
  activeItem = signal<string | null>(null);
  vasHistory = signal<any[]>([]);
  showHelp = signal(false);

  onItemIdentified(id: string) {
    this.activeItem.set(id.toUpperCase());
    this.mobile.notifyScan();
  }

  apply(service: string) {
    this.vasHistory.update(h => [{ id: this.activeItem(), service, time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }, ...h]);
    this.activeItem.set(null);
    this.mobile.notifySuccess();
  }
}
