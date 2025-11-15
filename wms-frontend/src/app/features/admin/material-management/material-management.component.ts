import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-material-management',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './material-management.component.html',
  styleUrls: ['./material-management.component.scss'],
})
export class MaterialManagementComponent {}
