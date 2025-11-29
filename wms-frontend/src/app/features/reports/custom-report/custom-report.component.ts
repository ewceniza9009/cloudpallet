import {
  Component,
  OnInit,
  inject,
  signal,
  ViewChild,
  ElementRef,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormControl,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, startWith } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../../../environments/environment';
import { ReportFilterDto, ReportType } from '../reports-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { MatTooltipModule } from '@angular/material/tooltip';

interface LookupDto {
  id: string;
  name: string;
}
interface UserLookupDto {
  id: string;
  name: string;
}
interface MaterialLookupDto {
  id: string;
  name: string;
  sku: string;
}

interface FullUserDto {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
}

@Component({
  selector: 'app-custom-report',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './custom-report.component.html',
  styleUrls: ['./custom-report.component.scss'],
})
export class CustomReportComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private sanitizer = inject(DomSanitizer);
  private authService = inject(AuthService);

  filterForm = this.fb.group({
    reportType: ['Receiving' as ReportType, Validators.required],
    dateRange: this.fb.group({
      startDate: [new Date(), Validators.required],
      endDate: [new Date(), Validators.required],
    }),
    accountId: [null as string | null],
    materialId: [null as string | null],
    supplierId: [null as string | null],
    userId: [null as string | null],
  });

  reportUrl = signal<SafeResourceUrl | null>(null);
  isLoading = signal(false);

  accountControl = new FormControl<string | LookupDto | null>(null);
  materialControl = new FormControl<string | MaterialLookupDto | null>(null);
  supplierControl = new FormControl<string | LookupDto | null>(null);
  userControl = new FormControl<string | UserLookupDto | null>(null);

  accounts = signal<LookupDto[]>([]);
  materials = signal<MaterialLookupDto[]>([]);
  suppliers = signal<LookupDto[]>([]);
  users = signal<UserLookupDto[]>([]);

  filteredAccounts$!: Observable<LookupDto[]>;
  filteredMaterials$!: Observable<MaterialLookupDto[]>;
  filteredSuppliers$!: Observable<LookupDto[]>;
  filteredUsers$!: Observable<UserLookupDto[]>;

  reportTypes: ReportType[] = [
    'Receiving',
    'Putaway',
    'Transfer',
    'Picking',
    'Shipping',
    'Invoice',
    'VAS',
    'VAS_Amend',
  ];

  @ViewChild('pdfFrame') pdfFrame!: ElementRef<HTMLIFrameElement>;
  isFullscreen = signal(false);

  ngOnInit(): void {
    this.loadLookups();
    this.setupFilters();

    this.addFullscreenListeners();
  }

  ngOnDestroy(): void {
    this.removeFullscreenListeners();
  }

  private addFullscreenListeners(): void {
    document.addEventListener('fullscreenchange', this.onFullscreenChange);
    document.addEventListener(
      'webkitfullscreenchange',
      this.onFullscreenChange
    );
    document.addEventListener('mozfullscreenchange', this.onFullscreenChange);
    document.addEventListener('MSFullscreenChange', this.onFullscreenChange);
  }

  private removeFullscreenListeners(): void {
    document.removeEventListener('fullscreenchange', this.onFullscreenChange);
    document.removeEventListener(
      'webkitfullscreenchange',
      this.onFullscreenChange
    );
    document.removeEventListener(
      'mozfullscreenchange',
      this.onFullscreenChange
    );
    document.removeEventListener('MSFullscreenChange', this.onFullscreenChange);
  }

  private onFullscreenChange = (): void => {
    this.isFullscreen.set(
      !!(
        document.fullscreenElement ||
        (document as any).webkitFullscreenElement ||
        (document as any).mozFullScreenElement ||
        (document as any).msFullscreenElement
      )
    );
  };

  loadLookups(): void {
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe((res) => {
        this.accounts.set(res);
        this.accountControl.updateValueAndValidity({ emitEvent: true });
      });
    this.http
      .get<MaterialLookupDto[]>(`${environment.apiUrl}/Lookups/materials`)
      .subscribe((res) => {
        this.materials.set(res);
        this.materialControl.updateValueAndValidity({ emitEvent: true });
      });
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/suppliers`)
      .subscribe((res) => {
        this.suppliers.set(res);
        this.supplierControl.updateValueAndValidity({ emitEvent: true });
      });

    this.http
      .get<FullUserDto[]>(`${environment.apiUrl}/Admin/users`)
      .subscribe((res) => {
        this.users.set(
          res.map((u) => ({ id: u.id, name: `${u.firstName} ${u.lastName}` }))
        );
        this.userControl.updateValueAndValidity({ emitEvent: true });
      });
  }

  setupFilters(): void {
    this.filteredAccounts$ = this.accountControl.valueChanges.pipe(
      startWith(''),
      map((value) => this._filter(value, this.accounts()))
    );
    this.filteredMaterials$ = this.materialControl.valueChanges.pipe(
      startWith(''),
      map((value) => this._filter(value, this.materials(), true))
    );
    this.filteredSuppliers$ = this.supplierControl.valueChanges.pipe(
      startWith(''),
      map((value) => this._filter(value, this.suppliers()))
    );
    this.filteredUsers$ = this.userControl.valueChanges.pipe(
      startWith(''),
      map((value) => this._filter(value, this.users()))
    );
  }

  getDisplayName(item: { name: string } | string | null): string {
    if (!item || typeof item === 'string') return item || '';
    return item.name;
  }

  getMaterialDisplayName(item: MaterialLookupDto | string | null): string {
    if (!item || typeof item === 'string') return item || '';
    return `${item.name} (${item.sku})`;
  }

  private _filter(
    value: string | { name: string } | null,
    options: any[],
    isMaterial: boolean = false
  ): any[] {
    const filterValue = (
      typeof value === 'string'
        ? value
        : (value as { name: string })?.name || ''
    ).toLowerCase();
    return options.filter(
      (option) =>
        option.name.toLowerCase().includes(filterValue) ||
        (isMaterial && option.sku.toLowerCase().includes(filterValue))
    );
  }

  onOptionSelected(
    event: MatAutocompleteSelectedEvent,
    formControl: FormControl
  ): void {
    formControl.setValue((event.option.value as LookupDto).id);
  }

  generateReport(): void {
    if (this.filterForm.invalid) {
      return;
    }

    this.isLoading.set(true);
    this.reportUrl.set(null);

    this.filterForm.controls.accountId.setValue(
      this.getControlId(this.accountControl)
    );
    this.filterForm.controls.materialId.setValue(
      this.getControlId(this.materialControl)
    );
    this.filterForm.controls.supplierId.setValue(
      this.getControlId(this.supplierControl)
    );
    this.filterForm.controls.userId.setValue(
      this.getControlId(this.userControl)
    );

    const formVal = this.filterForm.getRawValue();

    let params = new HttpParams()
      .set('reportType', formVal.reportType!)
      .set(
        'startDate',
        (formVal.dateRange.startDate ?? new Date()).toISOString()
      )
      .set('endDate', (formVal.dateRange.endDate ?? new Date()).toISOString());

    if (formVal.accountId) params = params.set('accountId', formVal.accountId);
    if (formVal.materialId)
      params = params.set('materialId', formVal.materialId);
    if (formVal.supplierId)
      params = params.set('supplierId', formVal.supplierId);
    if (formVal.userId) params = params.set('userId', formVal.userId);

    const token = this.authService.getToken();
    if (token) {
      params = params.set('access_token', token);
    }

    const url = `${environment.apiUrl}/reports/custom?${params.toString()}`;

    this.reportUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
  }

  private getControlId(control: FormControl<any>): string | null {
    const value = control.value;
    return value && typeof value !== 'string' ? value.id : null;
  }

  toggleFullscreen(): void {
    if (!this.pdfFrame) return;

    const elem = this.pdfFrame.nativeElement;

    if (
      !document.fullscreenElement &&
      !(document as any).webkitFullscreenElement &&
      !(document as any).mozFullScreenElement &&
      !(document as any).msFullscreenElement
    ) {
      if (elem.requestFullscreen) {
        elem.requestFullscreen();
      } else if ((elem as any).webkitRequestFullscreen) {
        (elem as any).webkitRequestFullscreen();
      } else if ((elem as any).mozRequestFullScreen) {
        (elem as any).mozRequestFullScreen();
      } else if ((elem as any).msRequestFullscreen) {
        (elem as any).msRequestFullscreen();
      }
    } else {
      if (document.exitFullscreen) {
        document.exitFullscreen();
      } else if ((document as any).webkitExitFullscreen) {
        (document as any).webkitExitFullscreen();
      } else if ((document as any).mozCancelFullScreen) {
        (document as any).mozCancelFullScreen();
      } else if ((document as any).msExitFullscreen) {
        (document as any).msExitFullscreen();
      }
    }
  }
}
