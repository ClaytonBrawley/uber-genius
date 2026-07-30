import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

type SortBy = 'startTime' | 'earnings' | 'distance';
type SortDir = 'asc' | 'desc';

interface TripListItem {
  id: number;
  startTimeUtc: string;
  endTimeUtc: string;
  city: string | null;
  status: string | null;
  distanceMiles: number;
  earnings: number;
  earningsMatchQuality: 'Unmatched' | 'Approximate' | 'Confident' | 'Cancelled';
}

interface TripListResult {
  items: TripListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

const API_BASE_URL = 'http://localhost:5269';
const PAGE_SIZE = 50;

@Component({
  selector: 'app-trips-page',
  imports: [],
  templateUrl: './trips-page.html',
})
export class TripsPage {
  private readonly http = inject(HttpClient);

  protected readonly page = signal(1);
  protected readonly sortBy = signal<SortBy>('startTime');
  protected readonly sortDir = signal<SortDir>('desc');
  protected readonly result = signal<TripListResult | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.totalCount / r.pageSize)) : 1;
  });

  constructor() {
    this.load();
  }

  protected sortByColumn(column: SortBy): void {
    if (this.sortBy() === column) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDir.set('desc');
    }
    this.page.set(1);
    this.load();
  }

  protected goToPage(delta: number): void {
    const next = this.page() + delta;
    if (next < 1 || next > this.totalPages()) {
      return;
    }
    this.page.set(next);
    this.load();
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  protected formatEarnings(value: number): string {
    return value.toLocaleString(undefined, { style: 'currency', currency: 'USD' });
  }

  protected durationLabel(item: TripListItem): string {
    const ms = new Date(item.endTimeUtc).getTime() - new Date(item.startTimeUtc).getTime();
    if (ms <= 0) {
      return '—';
    }

    const totalMinutes = Math.round(ms / 60000);
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
  }

  protected formatStatus(status: string | null): string {
    if (!status) {
      return '—';
    }
    return status
      .split('_')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  protected matchBadgeClass(quality: string): string {
    switch (quality) {
      case 'Confident':
        return 'bg-success-surface text-success-ink';
      case 'Approximate':
        return 'bg-warning-surface text-warning-ink';
      case 'Cancelled':
        return 'bg-neutral-surface text-neutral-ink';
      default:
        return 'bg-danger-surface text-danger-ink';
    }
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    const params = new URLSearchParams({
      page: String(this.page()),
      pageSize: String(PAGE_SIZE),
      sortBy: this.sortBy(),
      sortDir: this.sortDir(),
    });

    this.http.get<TripListResult>(`${API_BASE_URL}/api/trips?${params}`).subscribe({
      next: (res) => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load trips.');
        this.loading.set(false);
      },
    });
  }
}
