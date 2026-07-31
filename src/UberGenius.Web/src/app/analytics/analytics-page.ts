import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface TripEarningsPoint {
  startTimeUtc: string;
  earnings: number;
}

interface DayBucket {
  label: string;
  total: number;
  tripCount: number;
}

const API_BASE_URL = 'http://localhost:5269';
const DAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

@Component({
  selector: 'app-analytics-page',
  imports: [],
  templateUrl: './analytics-page.html',
})
export class AnalyticsPage {
  private readonly http = inject(HttpClient);

  protected readonly points = signal<TripEarningsPoint[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly buckets = computed<DayBucket[]>(() => {
    const totals = new Array(7).fill(0);
    const counts = new Array(7).fill(0);

    for (const p of this.points()) {
      const day = new Date(p.startTimeUtc).getDay(); // local time, 0=Sun..6=Sat
      totals[day] += p.earnings;
      counts[day]++;
    }

    return DAY_LABELS.map((label, i) => ({ label, total: totals[i], tripCount: counts[i] }));
  });

  protected readonly maxTotal = computed(() => Math.max(1, ...this.buckets().map((b) => b.total)));

  constructor() {
    this.http.get<TripEarningsPoint[]>(`${API_BASE_URL}/api/trips/earnings-points`).subscribe({
      next: (res) => {
        this.points.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load earnings analytics.');
        this.loading.set(false);
      },
    });
  }

  protected formatCurrency(value: number): string {
    return value.toLocaleString(undefined, { style: 'currency', currency: 'USD', maximumFractionDigits: 0 });
  }

  protected barHeightPercent(total: number): number {
    return Math.max(2, (total / this.maxTotal()) * 100);
  }
}
