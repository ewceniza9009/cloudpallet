import { Component, signal, ViewChild, ElementRef, inject } from '@angular/core';
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
  selector: 'app-mobile-putaway',
  standalone: true,
  imports: [
    CommonModule, RouterModule, MatCardModule, MatButtonModule, 
    MatIconModule, FormsModule, MobileHeaderComponent, ScanHubComponent
  ],
  template: `
    <div class="mobile-container">
      <app-mobile-header 
        title="Putaway / Transfer" 
        [subtitle]="history().length + ' Moves Completed'"
        [showHelp]="true"
        (help)="showHelp.set(true)">
      </app-mobile-header>

      <div class="content-body">
        <app-scan-hub
          [title]="step() === 'LPN' ? 'SCAN PALLET' : 'SCAN LOCATION'"
          [description]="step() === 'LPN' ? 'Point your device at the LPN barcode on the pallet.' : 'Move the pallet and scan the target bin barcode.'"
          [icon]="step() === 'LPN' ? 'pallet' : 'location_on'"
          [placeholder]="step() === 'LPN' ? 'Enter LPN...' : 'Enter Location...'"
          (scan)="processInput($event)">
        </app-scan-hub>

        <div class="active-state scale-in" *ngIf="currentLPN()">
            <div class="state-card glow-border">
                <div class="state-row">
                    <span class="label">ACTIVE PALLET</span>
                    <span class="value">{{ currentLPN() }}</span>
                </div>
                <div class="state-progress">
                    <mat-icon class="pulse">south</mat-icon>
                    <span>GO TO TARGET BIN</span>
                </div>
            </div>
        </div>

        <div class="history-section" *ngIf="history().length > 0">
            <div class="section-label">RECENT MOVEMENTS</div>
            <div class="history-list">
                <div class="history-item slide-up" *ngFor="let entry of history()">
                    <div class="icon-wrap success"><mat-icon>check_circle</mat-icon></div>
                    <div class="history-info">
                        <span class="lpn">{{ entry.lpn }}</span>
                        <span class="loc">MOVED TO <strong>{{ entry.location }}</strong></span>
                    </div>
                    <span class="time">{{ entry.time }}</span>
                </div>
            </div>
        </div>
      </div>

      <!-- Workflow Help Overlay -->
      <div class="help-overlay animate-fade-in" *ngIf="showHelp()" (click)="showHelp.set(false)">
          <div class="help-card animate-slide-up" (click)="$event.stopPropagation()">
              <h2>Putaway Workflow</h2>
              <div class="guide-step">
                  <div class="num">1</div>
                  <div class="txt"><strong>Scan Pallet:</strong> Identify the pallet by its LPN label (license plate number).</div>
              </div>
              <div class="guide-step">
                  <div class="num">2</div>
                  <div class="txt"><strong>Scan Location:</strong> Move the pallet to its target bin/rack and scan the location barcode to confirm.</div>
              </div>
              <div class="guide-step">
                  <div class="num">3</div>
                  <div class="txt"><strong>Repeat:</strong> The system resets for the next pallet automatically!</div>
              </div>
              <button mat-raised-button color="primary" class="full-width" (click)="showHelp.set(false)">UNDERSTOOD</button>
          </div>
      </div>
    </div>
  `,
  styles: [`
    .mobile-container { height: 100vh; background: #020617; display: flex; flex-direction: column; color: white; overflow: hidden; }
    .content-body { flex: 1; overflow-y: auto; padding: 1.5rem; }

    .active-state { margin-top: 2rem; }
    .state-card { background: rgba(16, 185, 129, 0.05); padding: 1.5rem; border-radius: 28px; border: 2px solid rgba(16, 185, 129, 0.2); }
    .glow-border { box-shadow: 0 0 30px rgba(16, 185, 129, 0.1); }
    .state-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .state-progress { display: flex; align-items: center; gap: 0.75rem; font-weight: 900; color: #10b981; font-size: 0.9rem; letter-spacing: 0.05em; font-style: italic; }
    .label { font-size: 0.7rem; font-weight: 800; color: #64748b; letter-spacing: 0.1em; }
    .value { font-weight: 900; font-family: 'JetBrains Mono', monospace; font-size: 1.5rem; color: white; }

    .history-section { margin-top: 2.5rem; }
    .section-label { font-size: 0.7rem; font-weight: 900; color: #475569; letter-spacing: 0.15em; margin-bottom: 1rem; }
    
    .history-item { 
       background: rgba(30, 41, 59, 0.4); padding: 1.25rem; border-radius: 24px; display: flex; align-items: center; gap: 1.25rem; margin-bottom: 0.75rem; 
       border: 1px solid rgba(255,255,255,0.05);
       .icon-wrap { width: 44px; height: 44px; border-radius: 12px; background: rgba(16, 185, 129, 0.1); color: #10b981; display: flex; align-items: center; justify-content: center; }
    }
    .history-info { flex: 1; display: flex; flex-direction: column; .lpn { font-weight: 900; color: white; font-size: 1.1rem; } .loc { font-size: 0.8rem; color: #94a3b8; } }
    .time { font-size: 0.7rem; color: #475569; font-weight: 700; }

    .pulse { animation: pulse 2s infinite; }
    @keyframes pulse { 0%, 100% { transform: translateY(0); } 50% { transform: translateY(5px); } }

    .help-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(20px); z-index: 3000; display: flex; align-items: flex-end; }
    .help-card { background: #1e293b; width: 100%; padding: 2.5rem; border-radius: 40px 40px 0 0; border-top: 1px solid rgba(255,255,255,0.1); }
    .help-card h2 { font-size: 1.5rem; font-weight: 900; color: #f97316 !important; margin-bottom: 2rem; }
    .guide-step { display: flex; gap: 1.5rem; margin-bottom: 1.5rem; align-items: flex-start; }
    .guide-step .num { width: 32px; height: 32px; border-radius: 12px; background: #f97316; color: white; display: flex; align-items: center; justify-content: center; font-weight: 900; flex-shrink: 0; }
    .guide-step .txt { font-size: 0.95rem; color: #94a3b8; line-height: 1.5; strong { color: white; } }

    .scale-in { animation: scaleIn 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275); }
    .slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes scaleIn { from { transform: scale(0.9); opacity: 0; } to { transform: scale(1); opacity: 1; } }
    @keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
    .animate-fade-in { animation: fadeIn 0.3s ease-out; }
    .animate-slide-up { animation: slideUp 0.4s ease-out; }
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
  `]
})
export class MobilePutawayComponent {
  private mobile = inject(MobileService);
  step = signal<'LPN' | 'LOC'>('LPN');
  currentLPN = signal<string | null>(null);
  history = signal<any[]>([]);
  showHelp = signal(false);

  processInput(val: string) {
    if (this.step() === 'LPN') {
      this.currentLPN.set(val.toUpperCase());
      this.step.set('LOC');
      this.mobile.notifyScan();
    } else {
      this.history.update(h => [{
        lpn: this.currentLPN(),
        location: val.toUpperCase(),
        time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      }, ...h]);
      this.currentLPN.set(null);
      this.step.set('LPN');
      this.mobile.notifySuccess();
    }
  }
}
