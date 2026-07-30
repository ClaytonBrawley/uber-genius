import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ImportWizard } from './import-wizard/import-wizard';
import { ThemeService } from './theme/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ImportWizard],
  templateUrl: './app.html'
})
export class App {
  protected readonly theme = inject(ThemeService);
}
