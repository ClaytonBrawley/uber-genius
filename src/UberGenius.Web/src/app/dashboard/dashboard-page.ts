import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface TripSummary {
  totalTrips: number;
  totalEarnings: number;
  averageEarningsPerTrip: number;
  totalMiles: number;
  averageEarningsPerMile: number;
  estimatedHourlyEarnings: number;
  totalDrivingHours: number;
}

const API_BASE_URL = 'http://localhost:5269';

@Component({
  selector: 'app-dashboard-page',
  imports: [],
  templateUrl: './dashboard-page.html',
})
export class DashboardPage {
  private readonly http = inject(HttpClient);

  protected readonly summary = signal<TripSummary | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.http.get<TripSummary>(`${API_BASE_URL}/api/trips/summary`).subscribe({
      next: (res) => {
        this.summary.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load earnings summary.');
        this.loading.set(false);
      },
    });
  }

  protected formatCurrency(value: number): string {
    return value.toLocaleString(undefined, { style: 'currency', currency: 'USD' });
  }

  protected formatMiles(value: number): string {
    return `${value.toLocaleString(undefined, { maximumFractionDigits: 0 })} mi`;
  }
}
