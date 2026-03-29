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
        { label: 'Receiving', icon: 'input', route: '/mobile/receiving', color: 'linear-gradient(135deg, #43a047 0%, #1b5e20 100%)', iconColor: '#ffffff' },
        { label: 'Putaway', icon: 'place', route: '/mobile/putaway', color: 'linear-gradient(135deg, #fb8c00 0%, #ef6c00 100%)', iconColor: '#ffffff' },
        { label: 'Picking', icon: 'shopping_cart', route: '/mobile/picking', color: 'linear-gradient(135deg, #1e88e5 0%, #0d47a1 100%)', iconColor: '#ffffff' },
        { label: 'Inventory', icon: 'inventory_2', route: '/mobile/inventory', color: 'linear-gradient(135deg, #8e24aa 0%, #4a148c 100%)', iconColor: '#ffffff' },
        { label: 'VAS', icon: 'auto_fix_high', route: '/mobile/vas', color: 'linear-gradient(135deg, #ffb300 0%, #ff8f00 100%)', iconColor: '#ffffff' },
        { label: 'Shipping', icon: 'local_shipping', route: '/mobile/shipping', color: 'linear-gradient(135deg, #00897b 0%, #004d40 100%)', iconColor: '#ffffff' },
    ];
}
// Refreshed
