import { AfterViewInit, Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as L from 'leaflet';

interface TripMapPoint {
  id: number;
  startTimeUtc: string;
  earnings: number;
  city: string | null;
  pickupLatitude: number | null;
  pickupLongitude: number | null;
  dropoffLatitude: number | null;
  dropoffLongitude: number | null;
}

const API_BASE_URL = 'http://localhost:5269';
const BIRMINGHAM_FALLBACK: [number, number] = [33.5207, -86.8025];

@Component({
  selector: 'app-map-page',
  imports: [],
  templateUrl: './map-page.html',
})
export class MapPage implements AfterViewInit {
  private readonly http = inject(HttpClient);
  private map?: L.Map;

  @ViewChild('mapContainer') private mapContainerRef!: ElementRef<HTMLDivElement>;

  protected readonly points = signal<TripMapPoint[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngAfterViewInit(): void {
    this.http.get<TripMapPoint[]>(`${API_BASE_URL}/api/trips/map-points`).subscribe({
      next: (res) => {
        this.points.set(res);
        this.loading.set(false);
        this.renderMap(res);
      },
      error: () => {
        this.error.set('Failed to load map data.');
        this.loading.set(false);
      },
    });
  }

  private renderMap(points: TripMapPoint[]): void {
    const first = points.find((p) => p.pickupLatitude != null) ?? points.find((p) => p.dropoffLatitude != null);
    const center: [number, number] = first
      ? [(first.pickupLatitude ?? first.dropoffLatitude)!, (first.pickupLongitude ?? first.dropoffLongitude)!]
      : BIRMINGHAM_FALLBACK;

    this.map = L.map(this.mapContainerRef.nativeElement).setView(center, 12);

    // Leaflet measures its container at construction time. The `loading…` text is still in
    // the DOM until this point, so the container may not have settled to its final 500px
    // height yet — without this, only a small top-left corner of tiles loads.
    setTimeout(() => this.map?.invalidateSize(), 0);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 19,
    }).addTo(this.map);

    const dot = (color: string) =>
      L.divIcon({
        className: '',
        html: `<div style="width:10px;height:10px;border-radius:50%;background:${color};border:2px solid white;box-shadow:0 0 2px rgba(0,0,0,0.5);"></div>`,
        iconSize: [10, 10],
      });

    const pickupIcon = dot('#16a34a');
    const dropoffIcon = dot('#dc2626');

    for (const p of points) {
      const date = new Date(p.startTimeUtc).toLocaleDateString();
      const earnings = p.earnings.toLocaleString(undefined, { style: 'currency', currency: 'USD' });

      if (p.pickupLatitude != null && p.pickupLongitude != null) {
        L.marker([p.pickupLatitude, p.pickupLongitude], { icon: pickupIcon })
          .addTo(this.map)
          .bindPopup(`<strong>Pickup</strong><br>${date} — ${earnings}`);
      }

      if (p.dropoffLatitude != null && p.dropoffLongitude != null) {
        L.marker([p.dropoffLatitude, p.dropoffLongitude], { icon: dropoffIcon })
          .addTo(this.map)
          .bindPopup(`<strong>Drop-off</strong><br>${date} — ${earnings}`);
      }
    }
  }
}
