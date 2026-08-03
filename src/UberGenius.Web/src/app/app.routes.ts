import { Routes } from '@angular/router';
import { AnalyticsPage } from './analytics/analytics-page';
import { authGuard, redirectIfAuthenticatedGuard } from './auth/auth.guard';
import { LoginPage } from './auth/login-page/login-page';
import { SignupPage } from './auth/signup-page/signup-page';
import { DashboardPage } from './dashboard/dashboard-page';
import { ImportWizard } from './import-wizard/import-wizard';
import { MapPage } from './map/map-page';
import { TripsPage } from './trips/trips-page';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginPage, canActivate: [redirectIfAuthenticatedGuard] },
  { path: 'signup', component: SignupPage, canActivate: [redirectIfAuthenticatedGuard] },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardPage },
      { path: 'trips', component: TripsPage },
      { path: 'analytics', component: AnalyticsPage },
      { path: 'map', component: MapPage },
      { path: 'import', component: ImportWizard },
    ],
  },
];
