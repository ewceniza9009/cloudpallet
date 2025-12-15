import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { AuthService } from '../../../core/services/auth.service';

@Component({
    selector: 'app-mobile-layout',
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        MatToolbarModule,
        MatButtonModule,
        MatIconModule,
        MatSidenavModule,
        MatListModule,
    ],
    templateUrl: './mobile-layout.component.html',
    styleUrls: ['./mobile-layout.component.scss'],
})
export class MobileLayoutComponent {
    private authService = inject(AuthService);
    private router = inject(Router);

    logout(): void {
        this.authService.logout();
        this.router.navigate(['/login']);
    }
}
