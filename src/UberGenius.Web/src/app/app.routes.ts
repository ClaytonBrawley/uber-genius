import { Routes } from '@angular/router';
import { AnalyticsPage } from './analytics/analytics-page';
import { DashboardPage } from './dashboard/dashboard-page';
import { ImportWizard } from './import-wizard/import-wizard';
import { TripsPage } from './trips/trips-page';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardPage },
  { path: 'trips', component: TripsPage },
  { path: 'analytics', component: AnalyticsPage },
  { path: 'import', component: ImportWizard },
];
