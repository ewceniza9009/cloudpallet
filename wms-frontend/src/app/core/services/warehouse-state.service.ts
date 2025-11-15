
import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class WarehouseStateService {

  public selectedWarehouseId = signal<string | null>(null);

  constructor() {
    const storedWarehouseId = localStorage.getItem('selectedWarehouseId');
    if (storedWarehouseId) {
      this.selectedWarehouseId.set(storedWarehouseId);
    }
  }

  setSelectedWarehouseId(id: string) {
    this.selectedWarehouseId.set(id);
    localStorage.setItem('selectedWarehouseId', id);
  }
}
