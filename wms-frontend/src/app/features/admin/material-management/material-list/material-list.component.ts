

import { Component, OnInit, inject, signal, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminApiService, MaterialDetailDto, GetMaterialsQuery } from '../../admin-api.service';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule, SortDirection } from '@angular/material/sort';
import { Subject, Subscription, merge, startWith, switchMap, map, catchError, of, debounceTime, distinctUntilChanged, tap } from 'rxjs';
import { PagedResult } from '../../../../core/models/paged-result.dto';
import { MatSnackBar } from '@angular/material/snack-bar';


@Component({
  selector: 'app-material-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    CurrencyPipe,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatPaginatorModule,
    MatSortModule,
  ],
  templateUrl: './material-list.component.html',
  styleUrls: ['./material-list.component.scss'],
})
export class MaterialListComponent implements OnInit, AfterViewInit, OnDestroy {
  private adminApi = inject(AdminApiService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  isLoading = signal(true);
  resultsLength = signal(0);
  searchControl = new FormControl('');

  dataSource = new MatTableDataSource<MaterialDetailDto>([]);
  displayedColumns: string[] = [
    'name',
    'type',
    'temperature',
    'cost',
    'weight',
    'flags',
    'actions',
  ];

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  private tableChangesSubscription: Subscription = new Subscription();
  refreshTrigger = new Subject<void>();

  ngOnInit(): void {

  }

  ngAfterViewInit(): void {

    if (!this.sort || !this.paginator) {
        console.error("MatSort or MatPaginator not found! This might be due to an *ngIf or @defer.");
        this.isLoading.set(false);
        return;
    }

    this.sort.active = 'name';
    this.sort.direction = 'asc';

    this.sort.sortChange.pipe(tap(() => this.paginator.pageIndex = 0)).subscribe();

    this.tableChangesSubscription = merge(
      this.sort.sortChange,
      this.paginator.page,
      this.searchControl.valueChanges.pipe(
          debounceTime(400),
          distinctUntilChanged(),
          tap(() => this.paginator.pageIndex = 0)
      ),
      this.refreshTrigger
    )
      .pipe(
        startWith({}),
        tap(() => this.isLoading.set(true)),
        switchMap(() => {
          const query: GetMaterialsQuery = {
            page: this.paginator.pageIndex + 1,
            pageSize: this.paginator.pageSize,
            sortBy: this.sort.active || 'name',
            sortDirection: this.sort.direction || 'asc',
            searchTerm: this.searchControl.value || undefined
          };

          return this.adminApi.getPagedMaterials(query).pipe(
            catchError((err) => {
              console.error('Failed to load materials:', err);
              this.snackBar.open('Failed to load materials. Please try again.', 'Close', { duration: 5000 });
              return of(null);
            })
          );
        }),

        map(data => {
          this.isLoading.set(false);

          if (data === null) {
            this.resultsLength.set(0);
            return [];
          }

          this.resultsLength.set(data.totalCount);
          return data.items;
        })
      )
      .subscribe(data => {
        this.dataSource.data = data;
      });
  }

  ngOnDestroy(): void {
    this.tableChangesSubscription.unsubscribe();
    this.refreshTrigger.complete();
  }

  createNew(): void {
    this.router.navigate(['/admin/materials/detail', 'new']);
  }

  getMaterialTypeClass(materialType: string): string {
    return materialType.toLowerCase();
  }
}
