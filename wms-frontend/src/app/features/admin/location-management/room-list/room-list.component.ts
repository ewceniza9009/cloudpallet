import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminSetupApiService, RoomDto } from '../../admin-setup-api.service';
import { map, startWith, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-room-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    DecimalPipe,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './room-list.component.html',
  styleUrls: ['./room-list.component.scss'],
})
export class RoomListComponent implements OnInit {
  private adminSetupApi = inject(AdminSetupApiService);
  private router = inject(Router);

  rooms = signal<RoomDto[]>([]);
  filteredRooms = signal<RoomDto[]>([]);
  isLoading = signal(true);
  searchControl = new FormControl('');

  ngOnInit(): void {
    this.loadData();

    this.searchControl.valueChanges.pipe(
      startWith(''),
      debounceTime(300),
      distinctUntilChanged(),
      map(value => this._filter(value || ''))
    ).subscribe(filtered => this.filteredRooms.set(filtered));
  }

  loadData(): void {
    this.isLoading.set(true);
    this.adminSetupApi.getAllRooms().subscribe({
      next: (data) => {
        this.rooms.set(data);
        this.filteredRooms.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load rooms:', error);
        this.isLoading.set(false);
      }
    });
  }

  private _filter(value: string): RoomDto[] {
    const filterValue = value.toLowerCase();
    return this.rooms().filter(room =>
      room.name.toLowerCase().includes(filterValue) ||
      room.serviceType.toLowerCase().includes(filterValue) ||
      room.minTemp.toString().includes(filterValue) ||
      room.maxTemp.toString().includes(filterValue)
    );
  }

  createNew(): void {
    this.router.navigate(['/setup/rooms/detail', 'new']);
  }

  getServiceTypeClass(serviceType: string): string {
    return serviceType.toLowerCase().replace(/\s+/g, '');
  }

  getLocationDensity(room: RoomDto): string {
    if (!room.locationCount || room.locationCount === 0) return 'None';
    if (room.locationCount < 10) return 'Low';
    if (room.locationCount < 50) return 'Medium';
    if (room.locationCount < 100) return 'High';
    return 'Very High';
  }
}
