// ---- File: wms-frontend/src/app/features/admin/warehouse-management/warehouse-list/warehouse-list.component.ts ----

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminSetupApiService, WarehouseDto } from '../../admin-setup-api.service';
import { map, startWith } from 'rxjs';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-warehouse-list',
  standalone: true,
  imports: [
    CommonModule, RouterModule, ReactiveFormsModule, MatListModule,
    MatFormFieldModule, MatInputModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './warehouse-list.component.html',
  styleUrls: ['./warehouse-list.component.scss']
})
export class WarehouseListComponent implements OnInit {
  private adminSetupApi = inject(AdminSetupApiService);
  private router = inject(Router);

  warehouses = signal<WarehouseDto[]>([]);
  filteredWarehouses = signal<WarehouseDto[]>([]);
  isLoading = signal(true);
  searchControl = new FormControl('');

  ngOnInit(): void {
    this.loadData();
    this.searchControl.valueChanges.pipe(
      startWith(''),
      map(value => this._filter(value || ''))
    ).subscribe(filtered => this.filteredWarehouses.set(filtered));
  }

  loadData(): void {
    this.isLoading.set(true);
    this.adminSetupApi.getWarehouses().subscribe(data => {
      this.warehouses.set(data);
      this.filteredWarehouses.set(data);
      this.isLoading.set(false);
    });
  }

  private _filter(value: string): WarehouseDto[] {
    const filterValue = value.toLowerCase();
    return this.warehouses().filter(wh =>
      wh.name.toLowerCase().includes(filterValue) ||
      wh.address.city.toLowerCase().includes(filterValue)
    );
  }

  createNew(): void {
    this.router.navigate(['/setup/warehouses/new']);
  }
}
