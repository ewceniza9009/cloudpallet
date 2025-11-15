import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-carrier-management',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule
  ],
  templateUrl: './carrier-management.component.html',
  styleUrls: ['./carrier-management.component.scss']
})
export class CarrierManagementComponent { }
