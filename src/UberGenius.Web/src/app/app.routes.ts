import { Routes } from '@angular/router';
import { ImportWizard } from './import-wizard/import-wizard';
import { TripsPage } from './trips/trips-page';

export const routes: Routes = [
  { path: '', redirectTo: 'trips', pathMatch: 'full' },
  { path: 'trips', component: TripsPage },
  { path: 'import', component: ImportWizard },
];
