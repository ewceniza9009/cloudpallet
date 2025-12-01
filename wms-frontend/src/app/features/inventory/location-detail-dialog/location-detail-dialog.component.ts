import { Component, Inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import {
  InventoryApiService,
  LocationDetailsDto,
  PalletMovementDto,
} from '../inventory-api.service';
import { LocationDto } from '../../warehouse/warehouse-api.service';

@Component({
  selector: 'app-location-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatCardModule,
    MatListModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatChipsModule,
    DatePipe,
    DecimalPipe,
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <h2 mat-dialog-title>
          <mat-icon class="header-icon">location_on</mat-icon>
          <span class="header-text">Location {{ data.barcode }}</span>
        </h2>
        <button mat-icon-button mat-dialog-close class="close-btn">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <mat-dialog-content>
        @if (isLoading()) {
          <div class="loading-container">
            <mat-progress-spinner mode="indeterminate" diameter="40"></mat-progress-spinner>
            <p>Loading details...</p>
          </div>
        } @else if (details(); as loc) {
          <mat-tab-group dynamicHeight mat-stretch-tabs="false" animationDuration="0ms" class="custom-tabs">
            <!-- DETAILS TAB -->
            <mat-tab label="Overview">
              <div class="tab-content">
                
                <!-- Status Section -->
                <div class="status-section">
                  <div class="status-card">
                    <span class="label">Zone Type</span>
                    <span class="value">{{ loc.zoneType }}</span>
                  </div>
                  <div class="status-card">
                    <span class="label">Utilization</span>
                    <span class="value" [class.high-util]="loc.utilization > 90">
                      {{ loc.utilization | number : '1.0-2' }}%
                    </span>
                  </div>
                  <div class="status-card">
                    <span class="label">Status</span>
                    <span class="status-badge" [ngClass]="loc.status.toLowerCase()">
                      {{ loc.status }}
                    </span>
                  </div>
                </div>

                @if (loc.pallet) {
                  <!-- Pallet Card -->
                  <div class="pallet-ticket">
                    <div class="ticket-header">
                      <div class="ticket-icon">
                        <mat-icon>layers</mat-icon>
                      </div>
                      <div class="ticket-info">
                        <div class="ticket-title">Pallet: {{ loc.pallet.barcode }}</div>
                        <div class="ticket-subtitle">{{ loc.pallet.type }}</div>
                      </div>
                      <div class="ticket-weight">
                        <span class="weight-val">{{ loc.pallet.weight | number : '1.0-2' }}</span>
                        <span class="weight-unit">kg</span>
                      </div>
                    </div>

                    <div class="ticket-body">
                      <h3 class="contents-title">Contents</h3>
                      <div class="contents-list">
                        @for (item of loc.pallet.materials; track item.sku) {
                          <div class="content-item">
                            <div class="item-main">
                              <div class="item-name">{{ item.materialName }}</div>
                              <div class="item-meta">
                                <span class="sku">SKU: {{ item.sku }}</span>
                                <span class="separator">•</span>
                                <span class="batch">Batch: {{ item.batchNumber }}</span>
                              </div>
                            </div>
                            <div class="item-qty">
                              <span class="qty-val">{{ item.quantity | number }}</span>
                              <span class="qty-unit">units</span>
                            </div>
                          </div>
                        }
                      </div>
                    </div>
                  </div>
                } @else {
                  <div class="empty-state">
                    <mat-icon>check_circle_outline</mat-icon>
                    <p>This location is currently empty.</p>
                  </div>
                }
              </div>
            </mat-tab>

            <!-- HISTORY TAB -->
            @if (loc.pallet) {
              <mat-tab label="History">
                <div class="tab-content history-content">
                  @if (historyLoading()) {
                    <mat-progress-bar mode="indeterminate" class="history-loader"></mat-progress-bar>
                  }
                  
                  <div class="history-timeline">
                    @for (item of history(); track item.timestamp) {
                      <div class="timeline-item">
                        <div class="timeline-marker"></div>
                        <div class="timeline-content">
                          <div class="timeline-header">
                            <span class="event-type">{{ item.eventType }}</span>
                            <span class="event-time">{{ item.timestamp | date : 'medium' }}</span>
                          </div>
                          <div class="timeline-body">
                            <div class="event-location">Location: {{ item.location }}</div>
                            <div class="event-details">{{ item.details }}</div>
                          </div>
                        </div>
                      </div>
                    } @empty {
                      @if (!historyLoading()) {
                        <p class="no-history">No history available.</p>
                      }
                    }
                  </div>
                </div>
              </mat-tab>
            }
          </mat-tab-group>
        } @else {
          <div class="error-state">
            <mat-icon color="warn">error</mat-icon>
            <p>Failed to load location details.</p>
          </div>
        }
      </mat-dialog-content>
    </div>
  `,
  styles: [
    `
      .dialog-container {
        display: flex;
        flex-direction: column;
        height: 100%;
        max-height: 85vh;
        background: #fff;
      }

      .dialog-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 20px 24px;
        border-bottom: 1px solid #f0f0f0;
        background: #fff;

        h2 {
          margin: 0;
          display: flex;
          align-items: center;
          gap: 12px;
          font-size: 1.25rem;
          font-weight: 600;
          color: #1a1a1a;
          
          .header-icon {
            color: #1976d2;
          }
        }
        
        .close-btn {
          color: #666;
        }
      }

      mat-dialog-content {
        padding: 0 !important;
        flex: 1;
        overflow-y: auto;
        background: #f9fafb;
      }

      .loading-container, .empty-state, .error-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 60px;
        gap: 16px;
        color: #666;

        mat-icon {
          font-size: 48px;
          width: 48px;
          height: 48px;
          opacity: 0.5;
        }
      }

      .tab-content {
        padding: 24px;
        display: flex;
        flex-direction: column;
        gap: 24px;
      }

      /* Status Section */
      .status-section {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 16px;
      }

      .status-card {
        background: #fff;
        padding: 16px;
        border-radius: 8px;
        border: 1px solid #e0e0e0;
        display: flex;
        flex-direction: column;
        gap: 8px;
        
        .label {
          font-size: 0.75rem;
          text-transform: uppercase;
          letter-spacing: 0.5px;
          color: #666;
          font-weight: 600;
        }
        
        .value {
          font-size: 1.25rem;
          font-weight: 600;
          color: #333;
          
          &.high-util {
            color: #d32f2f;
          }
        }
        
        .status-badge {
          align-self: flex-start;
          padding: 4px 12px;
          border-radius: 16px;
          font-size: 0.875rem;
          font-weight: 500;
          
          &.empty { background: #e8f5e9; color: #2e7d32; }
          &.partial { background: #e3f2fd; color: #1565c0; }
          &.approaching { background: #fff3e0; color: #ef6c00; }
          &.full, &.over { background: #ffebee; color: #c62828; }
        }
      }

      /* Pallet Ticket */
      .pallet-ticket {
        background: #fff;
        border-radius: 12px;
        border: 1px solid #e0e0e0;
        overflow: hidden;
        box-shadow: 0 2px 8px rgba(0,0,0,0.04);
      }

      .ticket-header {
        padding: 20px;
        background: #f8f9fa;
        border-bottom: 1px solid #e0e0e0;
        display: flex;
        align-items: center;
        gap: 16px;
        
        .ticket-icon {
          width: 48px;
          height: 48px;
          border-radius: 8px;
          background: #fff;
          display: flex;
          align-items: center;
          justify-content: center;
          border: 1px solid #e0e0e0;
          color: #555;
        }
        
        .ticket-info {
          flex: 1;
          
          .ticket-title {
            font-size: 1.1rem;
            font-weight: 600;
            color: #333;
            margin-bottom: 4px;
          }
          
          .ticket-subtitle {
            font-size: 0.9rem;
            color: #666;
          }
        }
        
        .ticket-weight {
          text-align: right;
          
          .weight-val {
            display: block;
            font-size: 1.5rem;
            font-weight: 700;
            color: #333;
            line-height: 1;
          }
          
          .weight-unit {
            font-size: 0.875rem;
            color: #888;
          }
        }
      }

      .ticket-body {
        padding: 20px;
        
        .contents-title {
          font-size: 0.875rem;
          text-transform: uppercase;
          letter-spacing: 1px;
          color: #888;
          margin: 0 0 16px 0;
          font-weight: 600;
        }
      }

      .contents-list {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .content-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 12px;
        background: #f8f9fa;
        border-radius: 6px;
        border: 1px solid #eee;
        
        .item-main {
          display: flex;
          flex-direction: column;
          gap: 4px;
          
          .item-name {
            font-weight: 500;
            color: #333;
          }
          
          .item-meta {
            font-size: 0.85rem;
            color: #666;
            display: flex;
            gap: 8px;
            
            .separator { color: #ccc; }
          }
        }
        
        .item-qty {
          text-align: right;
          
          .qty-val {
            font-weight: 600;
            color: #333;
            font-size: 1.1rem;
          }
          
          .qty-unit {
            font-size: 0.8rem;
            color: #888;
            margin-left: 4px;
          }
        }
      }

      /* History Timeline */
      .history-content {
        background: #fff;
        min-height: 300px;
      }

      .history-timeline {
        position: relative;
        padding-left: 20px;
        
        &::before {
          content: '';
          position: absolute;
          left: 7px;
          top: 0;
          bottom: 0;
          width: 2px;
          background: #f0f0f0;
        }
      }

      .timeline-item {
        position: relative;
        padding-bottom: 24px;
        padding-left: 24px;
        
        &:last-child {
          padding-bottom: 0;
        }
        
        .timeline-marker {
          position: absolute;
          left: -20px; /* Adjust based on padding-left of container */
          top: 4px;
          width: 12px;
          height: 12px;
          border-radius: 50%;
          background: #1976d2;
          border: 2px solid #fff;
          box-shadow: 0 0 0 2px #e3f2fd;
          z-index: 1;
        }
        
        .timeline-content {
          background: #f8f9fa;
          border-radius: 8px;
          padding: 12px 16px;
          border: 1px solid #eee;
          
          .timeline-header {
            display: flex;
            justify-content: space-between;
            margin-bottom: 8px;
            
            .event-type {
              font-weight: 600;
              color: #1976d2;
            }
            
            .event-time {
              font-size: 0.85rem;
              color: #888;
            }
          }
          
          .timeline-body {
            font-size: 0.9rem;
            color: #555;
            
            .event-location {
              margin-bottom: 4px;
              font-weight: 500;
            }
          }
        }
      }
      
      .no-history {
        text-align: center;
        color: #888;
        padding: 40px;
        font-style: italic;
      }
    `,
  ],
})
export class LocationDetailDialogComponent implements OnInit {
  isLoading = signal(true);
  details = signal<LocationDetailsDto | null>(null);
  
  historyLoading = signal(false);
  history = signal<PalletMovementDto[]>([]);
  historyColumns = ['timestamp', 'eventType', 'location', 'details'];

  constructor(
    public dialogRef: MatDialogRef<LocationDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LocationDto,
    private inventoryApi: InventoryApiService
  ) {}

  ngOnInit(): void {
    this.loadDetails();
  }

  loadDetails(): void {
    this.isLoading.set(true);
    this.inventoryApi.getLocationDetails(this.data.id).subscribe({
      next: (res) => {
        this.details.set(res);
        this.isLoading.set(false);
        
        if (res.pallet) {
          this.loadHistory(res.pallet.barcode);
        }
      },
      error: (err) => {
        console.error('Failed to load location details', err);
        this.isLoading.set(false);
      },
    });
  }

  loadHistory(palletBarcode: string): void {
    this.historyLoading.set(true);
    this.inventoryApi.getPalletHistory(palletBarcode).subscribe({
      next: (res) => {
        this.history.set(res);
        this.historyLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load pallet history', err);
        this.historyLoading.set(false);
      },
    });
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Over': return 'warn';
      case 'Full': return 'accent';
      case 'Approaching': return 'primary';
      default: return 'primary';
    }
  }
}
