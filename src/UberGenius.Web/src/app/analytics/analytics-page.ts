import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface DayOfWeekEarnings {
  label: string;
  total: number;
  tripCount: number;
}

const API_BASE_URL = 'http://localhost:5269';

@Component({
  selector: 'app-analytics-page',
  imports: [],
  templateUrl: './analytics-page.html',
})
export class AnalyticsPage {
  private readonly http = inject(HttpClient);

  protected readonly buckets = signal<DayOfWeekEarnings[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly maxTotal = computed(() => Math.max(1, ...this.buckets().map((b) => b.total)));

  constructor() {
    // Pre-bucketed server-side, using each trip's own timezone (or the account's default) and
    // Uber's real 4am/RequestedTime cutoff — not something a browser-local Date can replicate.
    this.http.get<DayOfWeekEarnings[]>(`${API_BASE_URL}/api/trips/earnings-by-day`).subscribe({
      next: (res) => {
        this.buckets.set(res);
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
