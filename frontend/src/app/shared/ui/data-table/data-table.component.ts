import { Component, Input, Output, EventEmitter, ContentChild, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
}

@Component({
  selector: 'app-data-table',
  imports: [CommonModule],
  template: `
    <div class="table-container">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              @for (column of columns; track column.key) {
                <th
                  [style.width]="column.width"
                  [class.text-center]="column.align === 'center'"
                  [class.text-right]="column.align === 'right'"
                  [class.sortable]="column.sortable"
                  (click)="column.sortable && onSort(column.key)"
                >
                  <div class="th-content">
                    <span>{{ column.label }}</span>
                    @if (column.sortable && sortKey === column.key) {
                      <span class="sort-icon" aria-hidden="true">
                        {{ sortDirection === 'asc' ? '↑' : '↓' }}
                      </span>
                    }
                  </div>
                </th>
              }
            </tr>
          </thead>
          <tbody>
            @if (loading) {
              <tr>
                <td [attr.colspan]="columns.length" class="loading-cell">
                  <div class="loading-state">
                    <span class="spinner" aria-hidden="true"></span>
                    <span>Loading...</span>
                  </div>
                </td>
              </tr>
            } @else if (data.length === 0) {
              <tr>
                <td [attr.colspan]="columns.length" class="empty-cell">
                  <div class="empty-state">
                    <span class="empty-icon" aria-hidden="true">📋</span>
                    <p class="empty-title">{{ emptyTitle }}</p>
                    <p class="empty-message">{{ emptyMessage }}</p>
                  </div>
                </td>
              </tr>
            } @else {
              @for (row of data; track trackBy(row); let i = $index) {
                <tr
                  [class.clickable]="rowClickable"
                  [class.selected]="isSelected(row)"
                  (click)="onRowClick(row)"
                >
                  <ng-container *ngTemplateOutlet="rowTemplate || defaultRow; context: { $implicit: row, index: i }" />
                </tr>
              }
            }
          </tbody>
        </table>
      </div>
      
      @if (paginated && !loading && data.length > 0) {
        <div class="table-footer">
          <div class="pagination-info">
            Showing {{ (currentPage - 1) * pageSize + 1 }} to 
            {{ Math.min(currentPage * pageSize, totalItems) }} of {{ totalItems }}
          </div>
          <div class="pagination-controls">
            <button
              type="button"
              [disabled]="currentPage === 1"
              (click)="onPageChange(currentPage - 1)"
              aria-label="Previous page"
            >
              Previous
            </button>
            <span class="page-indicator">Page {{ currentPage }} of {{ totalPages }}</span>
            <button
              type="button"
              [disabled]="currentPage === totalPages"
              (click)="onPageChange(currentPage + 1)"
              aria-label="Next page"
            >
              Next
            </button>
          </div>
        </div>
      }
    </div>

    <ng-template #defaultRow let-row>
      @for (column of columns; track column.key) {
        <td
          [class.text-center]="column.align === 'center'"
          [class.text-right]="column.align === 'right'"
        >
          {{ row[column.key] }}
        </td>
      }
    </ng-template>
  `,
  styles: `
    .table-container {
      border: 1px solid var(--border-default);
      border-radius: var(--radius-lg);
      background: var(--surface);
      overflow: hidden;
    }

    .table-wrapper {
      overflow-x: auto;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
      font-size: var(--font-size-sm);
    }

    thead {
      background: var(--canvas);
      border-bottom: 1px solid var(--border-default);
      position: sticky;
      top: 0;
      z-index: 10;
    }

    th {
      padding: var(--space-2) var(--space-4);
      text-align: left;
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-semibold);
      color: var(--text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      white-space: nowrap;
      user-select: none;
      height: var(--height-table-row);
    }

    .th-content {
      display: flex;
      align-items: center;
      gap: var(--space-1);
    }

    .sortable {
      cursor: pointer;
      transition: color var(--transition-fast);
    }

    .sortable:hover {
      color: var(--text-heading);
    }

    .sort-icon {
      font-size: var(--font-size-sm);
      color: var(--primary-copper);
    }

    tbody tr {
      border-bottom: 1px solid var(--border-light);
      transition: background var(--transition-fast);
    }

    tbody tr:hover {
      background: var(--canvas);
    }

    tbody tr.clickable {
      cursor: pointer;
    }

    tbody tr.selected {
      background: var(--surface-accent);
    }

    td {
      padding: var(--space-2) var(--space-4);
      color: var(--text-primary);
      vertical-align: middle;
      height: var(--height-table-row);
    }

    .text-center {
      text-align: center;
    }

    .text-right {
      text-align: right;
    }

    .loading-cell,
    .empty-cell {
      text-align: center;
      padding: var(--space-10) var(--space-4);
    }

    .loading-state,
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-2);
      color: var(--text-muted);
    }

    .spinner {
      display: inline-block;
      width: 20px;
      height: 20px;
      border: 2px solid var(--border-default);
      border-top-color: var(--primary-copper);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .empty-icon {
      font-size: 2.5rem;
      opacity: 0.5;
    }

    .empty-title {
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
      color: var(--text-heading);
      margin: 0;
    }

    .empty-message {
      font-size: var(--font-size-sm);
      color: var(--text-muted);
      margin: 0;
    }

    .table-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: var(--space-3) var(--space-4);
      border-top: 1px solid var(--border-light);
      background: var(--canvas);
      font-size: var(--font-size-sm);
    }

    .pagination-info {
      color: var(--text-secondary);
    }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }

    .pagination-controls button {
      padding: var(--space-1) var(--space-3);
      height: var(--height-button-sm);
      background: var(--surface);
      border: 1px solid var(--border-default);
      border-radius: var(--radius-md);
      color: var(--text-heading);
      font-size: var(--font-size-sm);
      cursor: pointer;
      transition: all var(--transition-fast);
    }

    .pagination-controls button:hover:not(:disabled) {
      background: var(--canvas);
      border-color: var(--border-medium);
    }

    .pagination-controls button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .page-indicator {
      color: var(--text-secondary);
      font-size: var(--font-size-sm);
    }

    @media (max-width: 768px) {
      .table-footer {
        flex-direction: column;
        gap: var(--space-2);
      }

      .pagination-info {
        order: 2;
      }

      .pagination-controls {
        order: 1;
      }
    }
  `,
})
export class DataTableComponent {
  @Input() columns: TableColumn[] = [];
  @Input() data: any[] = [];
  @Input() loading = false;
  @Input() emptyTitle = 'No data';
  @Input() emptyMessage = 'There are no records to display';
  @Input() rowClickable = false;
  @Input() paginated = true;
  @Input() pageSize = 50;
  @Input() currentPage = 1;
  @Input() totalItems = 0;
  @Input() sortKey: string | null = null;
  @Input() sortDirection: 'asc' | 'desc' = 'asc';

  @Output() rowClick = new EventEmitter<any>();
  @Output() sort = new EventEmitter<{ key: string; direction: 'asc' | 'desc' }>();
  @Output() pageChange = new EventEmitter<number>();

  @ContentChild('row') rowTemplate?: TemplateRef<any>;

  readonly Math = Math;

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize);
  }

  trackBy(item: any): any {
    return item.id || item;
  }

  isSelected(row: any): boolean {
    return false; // Can be extended for multi-select
  }

  onRowClick(row: any): void {
    if (this.rowClickable) {
      this.rowClick.emit(row);
    }
  }

  onSort(key: string): void {
    const direction = this.sortKey === key && this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.sort.emit({ key, direction });
  }

  onPageChange(page: number): void {
    this.pageChange.emit(page);
  }
}
