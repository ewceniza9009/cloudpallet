import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-rate-management',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './rate-management.component.html',
  styleUrls: ['./rate-management.component.scss'],
})
export class RateManagementComponent {}
