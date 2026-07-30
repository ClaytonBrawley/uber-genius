import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ImportWizard } from './import-wizard/import-wizard';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ImportWizard],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('UberGenius.Web');
}
