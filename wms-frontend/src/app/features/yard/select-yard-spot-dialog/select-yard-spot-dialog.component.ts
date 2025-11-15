import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { signal } from '@angular/core';
import { YardApiService, YardSpotDto } from '../yard-api.service';

@Component({
  selector: 'app-select-yard-spot-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './select-yard-spot-dialog.component.html',
})
export class SelectYardSpotDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private yardApi = inject(YardApiService);
  public dialogRef = inject(MatDialogRef<SelectYardSpotDialogComponent>);

  spots = signal<YardSpotDto[]>([]);
  isLoading = signal(true);

  form = this.fb.group({
    yardSpotId: ['', Validators.required],
  });

  ngOnInit(): void {
    this.yardApi.getAvailableYardSpots().subscribe({
      next: (data) => {
        this.spots.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  onConfirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value.yardSpotId);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
