import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

@Component({
    selector: 'app-mobile-menu',
    standalone: true,
    imports: [CommonModule, RouterModule, MatCardModule, MatIconModule],
    templateUrl: './mobile-menu.component.html',
    styleUrls: ['./mobile-menu.component.scss'],
})
export class MobileMenuComponent {
    authService = inject(AuthService);

    menuItems = [
        { label: 'Picking', icon: 'shopping_cart', route: '/mobile/picking', color: '#e3f2fd', iconColor: '#1976d2' },
        { label: 'Receiving', icon: 'input', route: '/mobile/receiving', color: '#e8f5e9', iconColor: '#388e3c' },
        { label: 'Putaway', icon: 'place', route: '/mobile/putaway', color: '#fff3e0', iconColor: '#f57c00' },
        { label: 'Inventory', icon: 'inventory_2', route: '/mobile/inventory', color: '#f3e5f5', iconColor: '#7b1fa2' },
    ];
}
// Refreshed
