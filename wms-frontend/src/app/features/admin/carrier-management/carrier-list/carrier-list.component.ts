import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminSetupApiService, CarrierDto } from '../../admin-setup-api.service';
import { map, startWith, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-carrier-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './carrier-list.component.html',
  styleUrls: ['./carrier-list.component.scss'],
})
export class CarrierListComponent implements OnInit {
  private adminSetupApi = inject(AdminSetupApiService);
  private router = inject(Router);

  carriers = signal<CarrierDto[]>([]);
  filteredCarriers = signal<CarrierDto[]>([]);
  isLoading = signal(true);
  searchControl = new FormControl('');

  ngOnInit(): void {
    this.loadData();

    this.searchControl.valueChanges.pipe(
      startWith(''),
      debounceTime(300),
      distinctUntilChanged(),
      map(value => this._filter(value || ''))
    ).subscribe(filtered => this.filteredCarriers.set(filtered));
  }

  loadData(): void {
    this.isLoading.set(true);
    this.adminSetupApi.getAllCarriers().subscribe({
      next: (data) => {
        this.carriers.set(data);
        this.filteredCarriers.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load carriers:', error);
        this.isLoading.set(false);
      }
    });
  }

  private _filter(value: string): CarrierDto[] {
    const filterValue = value.toLowerCase();
    return this.carriers().filter(carrier =>
      carrier.name.toLowerCase().includes(filterValue) ||
      carrier.scacCode.toLowerCase().includes(filterValue) ||
      carrier.address?.city?.toLowerCase().includes(filterValue) ||
      carrier.address?.state?.toLowerCase().includes(filterValue)
    );
  }

  createNew(): void {
    this.router.navigate(['/setup/carriers/detail', 'new']);
  }

  getFleetSize(carrier: CarrierDto): string {
    const truckCount = carrier.truckCount || 0;
    if (truckCount === 0) return 'None';
    if (truckCount < 5) return 'Small';
    if (truckCount < 20) return 'Medium';
    return 'Large';
  }
}
