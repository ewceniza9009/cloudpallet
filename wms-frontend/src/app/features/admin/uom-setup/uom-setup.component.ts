import { Component, OnInit, inject, signal, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { AdminSetupApiService, UnitOfMeasureDto, GetUnitOfMeasuresQuery, UnitOfMeasureDetailDto } from '../admin-setup-api.service';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog.component';

import { debounceTime, distinctUntilChanged, tap } from 'rxjs';
import { UomDialogComponent } from './uom-dialog/uom-dialog.component';

@Component({
  selector: 'app-uom-setup',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule,
    MatPaginatorModule, MatSortModule, MatTooltipModule
  ],
  templateUrl: './uom-setup.component.html',
  styleUrls: ['./uom-setup.component.scss']
})
export class UomSetupComponent implements OnInit, AfterViewInit {
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  isLoading = signal(false);
  dataSource = new MatTableDataSource<UnitOfMeasureDto>([]);
  displayedColumns = ['name', 'symbol', 'actions'];
  resultsLength = signal(0);
  searchControl = new FormControl('');

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  ngOnInit(): void {
    // Setup search with debounce
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
    // Load initial data
    this.loadData();

    // Setup sort and pagination
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

    // Convert SortDirection to the expected type
    const sortDirection = this.sort?.direction === 'asc' || this.sort?.direction === 'desc'
      ? this.sort.direction
      : undefined;

    const query: GetUnitOfMeasuresQuery = {
      page: this.paginator ? this.paginator.pageIndex + 1 : 1,
      pageSize: this.paginator ? this.paginator.pageSize : 10,
      sortBy: this.sort ? this.sort.active : 'name',
      sortDirection: sortDirection,
      searchTerm: this.searchControl.value || undefined
    };

    this.adminSetupApi.getPagedUoMs(query).subscribe({
      next: (data) => {
        this.resultsLength.set(data.totalCount);
        this.dataSource.data = data.items;
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load UoMs:', error);
        this.snackBar.open('Failed to load Units of Measure.', 'Close', { duration: 3000 });
        this.isLoading.set(false);
        this.dataSource.data = [];
      }
    });
  }

  refreshList(): void {
    this.loadData();
  }

  openDialog(uom?: UnitOfMeasureDetailDto): void {
    const dialogRef = this.dialog.open(UomDialogComponent, {
      width: '450px',
      data: { uom }
    });

    dialogRef.afterClosed().subscribe((result: boolean) => {
      if (result === true) {
        this.refreshList();
      }
    });
  }

  onDelete(uom: UnitOfMeasureDto): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Unit of Measure',
        message: `Are you sure you want to delete "${uom.name}" (${uom.symbol})? This cannot be undone.`
      }
    });

    dialogRef.afterClosed().subscribe((result: boolean) => {
      if (result === true) {
        this.adminSetupApi.deleteUoM(uom.id).subscribe({
          next: () => {
            this.snackBar.open('Unit of Measure deleted.', 'OK', { duration: 3000 });
            this.refreshList();
          },
          error: (err) => {
            this.snackBar.open(
              `Error: ${err.error?.title || 'Failed to delete.'}`,
              'Close'
            );
          }
        });
      }
    });
  }
}
