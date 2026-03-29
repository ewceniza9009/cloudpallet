import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core'; 
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-mobile-header',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatIconModule],
  template: `
    <header class="mobile-header-root">
        <button mat-icon-button class="back-btn" (click)="onBack()">
            <mat-icon>{{ backIcon }}</mat-icon>
        </button>
        
        <div class="header-titles">
            <h1>{{ title }}</h1>
            <span class="subtitle" *ngIf="subtitle">{{ subtitle }}</span>
        </div>

        <div class="spacer"></div>

        <div class="header-actions">
            <button mat-icon-button *ngIf="showSearch" (click)="searchToggled.emit(!searchActive); searchActive = !searchActive" [class.active]="searchActive">
                <mat-icon>{{ searchActive ? 'close' : 'search' }}</mat-icon>
            </button>
            <button mat-icon-button *ngIf="showRefresh" (click)="refresh.emit()" [disabled]="isLoading">
                <mat-icon [class.spinning]="isLoading">refresh</mat-icon>
            </button>
            <button mat-icon-button *ngIf="showHelp" (click)="help.emit()">
                <mat-icon>help_outline</mat-icon>
            </button>
        </div>
    </header>
  `,
  styles: [`
    .mobile-header-root {
        padding: 1.5rem 1rem; background: rgba(15, 23, 42, 0.85); backdrop-filter: blur(20px);
        color: white; display: flex; align-items: center; gap: 1rem; border-bottom: 1px solid rgba(255,255,255,0.08); 
        box-shadow: 0 4px 30px rgba(0,0,0,0.5); z-index: 1000; position: sticky; top: 0;
    }
    .back-btn { background: rgba(255,255,255,0.05) !important; color: white !important; border-radius: 12px; }
    .header-titles {
        flex: 1; display: flex; flex-direction: column; overflow: hidden;
        h1 { margin: 0; font-size: 1.2rem; font-weight: 900; color: white !important; line-height: 1.1; letter-spacing: -0.02em; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .subtitle { font-size: 0.75rem; font-weight: 700; color: var(--mat-sys-primary); letter-spacing: 0.05em; margin-top: 4px; opacity: 0.9; text-transform: uppercase; }
    }
    .spacer { flex: 1; }
    .header-actions { display: flex; align-items: center; gap: 4px; }
    .spinning { animation: rotate 1.5s linear infinite; }
    .active { color: var(--mat-sys-primary) !important; }
    @keyframes rotate { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
  `]
})
export class MobileHeaderComponent {
  @Input() title: string = '';
  @Input() subtitle: string = '';
  @Input() backLink: string = '/mobile/menu';
  @Input() backIcon: string = 'arrow_back';
  @Input() showHelp: boolean = false;
  @Input() showSearch: boolean = false;
  @Input() showRefresh: boolean = false;
  @Input() isLoading: boolean = false;

  @Output() help = new EventEmitter<void>();
  @Output() refresh = new EventEmitter<void>();
  @Output() searchToggled = new EventEmitter<boolean>();

  searchActive = false;
  private router = inject(Router);

  onBack(): void {
    if (this.backLink) {
      this.router.navigate([this.backLink]);
    }
  }
}
