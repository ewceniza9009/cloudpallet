
import { Component, OnInit, inject, signal, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { AdminSetupApiService, PalletTypeDto, GetPalletTypesQuery, PalletTypeDetailDto } from '../admin-setup-api.service';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog.component';
import { PalletTypeDialogComponent } from './pallet-type-dialog/pallet-type-dialog.component';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs';

@Component({
  selector: 'app-pallet-type-setup',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule,
    MatPaginatorModule, MatSortModule, MatTooltipModule, DecimalPipe
  ],
  templateUrl: './pallet-type-setup.component.html',
  styleUrls: ['./pallet-type-setup.component.scss']
})
export class PalletTypeSetupComponent implements OnInit, AfterViewInit {
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  isLoading = signal(false);
  dataSource = new MatTableDataSource<PalletTypeDto>([]);
  displayedColumns = ['name', 'tareWeight', 'dimensions', 'isActive', 'actions'];
  resultsLength = signal(0);
  searchControl = new FormControl('');

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngOnInit(): void {

    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      tap(() => {
        if (this.paginator) {
          this.paginator.firstPage();
        }
        this.loadData();
      })
    ).subscribe();
  }

  ngAfterViewInit(): void {

    this.loadData();


    this.sort.sortChange.subscribe(() => {
      if (this.paginator) {
        this.paginator.firstPage();
      }
      this.loadData();
    });

    this.paginator.page.subscribe(() => {
      this.loadData();
    });
  }

  loadData(): void {
    this.isLoading.set(true);


    const sortDirection = this.sort?.direction === 'asc' || this.sort?.direction === 'desc'
      ? this.sort.direction
      : undefined;

    const query: GetPalletTypesQuery = {
      page: this.paginator ? this.paginator.pageIndex + 1 : 1,
      pageSize: this.paginator ? this.paginator.pageSize : 10,
      sortBy: this.sort ? this.sort.active : 'name',
      sortDirection: sortDirection,
      searchTerm: this.searchControl.value || undefined
    };

    this.adminSetupApi.getPagedPalletTypes(query).subscribe({
      next: (data) => {
        this.resultsLength.set(data.totalCount);
        this.dataSource.data = data.items;
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load Pallet Types:', error);
        this.snackBar.open('Failed to load Pallet Types.', 'Close', { duration: 3000 });
        this.isLoading.set(false);
        this.dataSource.data = [];
      }
    });
  }

  refreshList(): void {
    this.loadData();
  }

  openDialog(palletType?: PalletTypeDetailDto): void {
    const dialogRef = this.dialog.open(PalletTypeDialogComponent, {
      width: '550px',
      data: { palletType }
    });

    dialogRef.afterClosed().subscribe((result: boolean) => {
      if (result === true) {
        this.refreshList();
      }
    });
  }

  onDelete(palletType: PalletTypeDto): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Pallet Type',
        message: `Are you sure you want to delete "${palletType.name}"? This cannot be undone.`
      }
    });

    dialogRef.afterClosed().subscribe((result: boolean) => {
      if (result === true) {
        this.adminSetupApi.deletePalletType(palletType.id).subscribe({
          next: () => {
            this.snackBar.open('Pallet Type deleted.', 'OK', { duration: 3000 });
            this.refreshList();
          },
          error: (err) => {
            const errorMsg = err.error?.title || err.error?.detail || 'Failed to delete Pallet Type.';

            if (errorMsg.includes('being used')) {
                 this.snackBar.open('Cannot delete: Pallet Type is currently in use.', 'Close', { duration: 5000 });
            } else {
                this.snackBar.open(`Error: ${errorMsg}`, 'Close', { duration: 5000 });
            }
          }
        });
      }
    });
  }
}
