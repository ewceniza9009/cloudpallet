# Virtual Scrolling Pattern for Autocomplete Dropdowns

This document provides a **copy-paste ready pattern** for adding virtual scrolling to `mat-autocomplete` dropdowns in Angular Material.

## Benefits

- **Performance**: Only renders ~10-20 visible items instead of ALL items
- **Memory**: Dramatically reduced DOM nodes (1000+ items → ~20 items)
- **Speed**: Faster initial render and smoother scrolling

## When to Use

Apply virtual scrolling when:
- ✅ Dropdown has **100+ options**
- ✅ Users select from accounts, materials, suppliers, or other large datasets
- ✅ Performance issues are noticeable (lag on dropdown open)

## Implementation Pattern

### 1. Add ScrollingModule Import

In your component TypeScript file:

```typescript
import { ScrollingModule } from '@angular/cdk/scrolling';

@Component({
  // ... other config
  imports: [
    // ... other imports
    ScrollingModule,  // Add this
  ],
})
```

### 2. Update HTML Template

Wrap your `@for` loop with `<cdk-virtual-scroll-viewport>`:

**BEFORE:**
```html
<mat-autocomplete #auto="matAutocomplete" [displayWith]="displayFn">
  @for(item of filteredItems$ | async; track item.id) {
    <mat-option [value]="item">{{ item.name }}</mat-option>
  }
</mat-autocomplete>
```

**AFTER:**
```html
<mat-autocomplete #auto="matAutocomplete" [displayWith]="displayFn">
  <cdk-virtual-scroll-viewport 
    itemSize="48" 
    minBufferPx="200" 
    maxBufferPx="400" 
    class="autocomplete-viewport">
    @for(item of filteredItems$ | async; track item.id) {
      <mat-option [value]="item">{{ item.name }}</mat-option>
    }
  </cdk-virtual-scroll-viewport>
</mat-autocomplete>
```

### 3. Parameters Explained

| Parameter | Value | Description |
|-----------|-------|-------------|
| `itemSize` | `48` | Height of each mat-option in pixels (Material default) |
| `minBufferPx` | `200` | Minimum pixels to render outside viewport |
| `maxBufferPx` | `400` | Maximum pixels to render outside viewport |
| `class` | `autocomplete-viewport` | CSS class for styling (already in global styles) |

## Complete Example

Here's a full working example from `SearchPalletDialogComponent`:

### TypeScript

```typescript
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { ScrollingModule } from '@angular/cdk/scrolling';  // 👈 Add this
import { Observable, startWith, map } from 'rxjs';

interface AccountDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    ScrollingModule,  // 👈 Add this
  ],
})
export class ExampleComponent implements OnInit {
  accountControl = new FormControl<AccountDto | string | null>(null);
  accounts = signal<AccountDto[]>([]);
  filteredAccounts$!: Observable<AccountDto[]>;

  ngOnInit(): void {
    // Setup filtering
    this.filteredAccounts$ = this.accountControl.valueChanges.pipe(
      startWith(''),
      map(value => this._filter(value))
    );
  }

  private _filter(value: string | AccountDto | null): AccountDto[] {
    const filterValue = (typeof value === 'string' ? value : value?.name || '').toLowerCase();
    return this.accounts().filter(acc => acc.name.toLowerCase().includes(filterValue));
  }

  displayAccountName(account: AccountDto): string {
    return account?.name || '';
  }
}
```

### HTML

```html
<mat-form-field appearance="outline">
  <mat-label>Account</mat-label>
  <input type="text"
         matInput
         [formControl]="accountControl"
         [matAutocomplete]="autoAccount">
  <mat-autocomplete #autoAccount="matAutocomplete"
                    [displayWith]="displayAccountName">
    <!-- 👇 Wrap with virtual scroll viewport -->
    <cdk-virtual-scroll-viewport 
      itemSize="48" 
      minBufferPx="200" 
      maxBufferPx="400" 
      class="autocomplete-viewport">
      @for(account of filteredAccounts$ | async; track account.id) {
        <mat-option [value]="account">{{ account.name }}</mat-option>
      }
    </cdk-virtual-scroll-viewport>
    <!-- 👆 -->
  </mat-autocomplete>
</mat-form-field>
```

## Global CSS

The `.autocomplete-viewport` class is defined in `styles.scss`:

```scss
.autocomplete-viewport {
  height: 256px; /* ~5-6 items visible */
  width: 100%;
}

.autocomplete-viewport .mat-mdc-option {
  height: 48px;
  min-height: 48px;
}
```

## Priority Components to Update

Based on usage analysis, these components would benefit most:

### High Priority (1000+ items)
1. **Kitting Component** - Material and Account dropdowns
2. **Repack Component** - Material and Account dropdowns  
3. **Picking Component** - Material and Account dropdowns
4. **Receiving Session** - Material, Account, and Supplier dropdowns

### Medium Priority (500+ items)
5. **Stock-on-Hand Report** - Account, Material, Supplier filters
6. **Inventory Ledger Report** - Account, Material, Supplier filters
7. **Custom Report** - Account, Material, Supplier, User filters

### Lower Priority (fewer items or less frequently used)
8. Activity Log filters
9. Rate Management selects
10. Material Management BOM inputs

## Testing

After implementation, verify:
- ✅ Dropdown opens quickly (no lag)
- ✅ Scrolling is smooth
- ✅ Search/filter still works correctly
- ✅ Selection works as expected
- ✅ Display value shows correctly when selected

## Troubleshooting

**Problem**: Options not showing  
**Solution**: Ensure `ScrollingModule` is imported

**Problem**: Viewport is too tall/short  
**Solution**: Adjust `height` in CSS (256px default)

**Problem**: Jumpy scrolling  
**Solution**: Ensure `itemSize="48"` matches actual option height

**Problem**: Not all items visible  
**Solution**: Check that your observable/signal is emitting all filtered items

## References

- [Angular CDK Virtual Scrolling](https://material.angular.io/cdk/scrolling/overview)
- [Example: Search Pallet Dialog Component](file:///x:/wms/wms-frontend/src/app/features/inventory/search-pallet-dialog/search-pallet-dialog.component.html)
