import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

type ImportCategory = 'driver-profile' | 'trips' | 'payments' | 'app-analytics';
type StepStatus = 'idle' | 'validating' | 'valid' | 'invalid';

interface CsvImportResult {
  succeeded: boolean;
  error: string | null;
  headersFound: string[];
  columnMapping: Record<string, string>;
  rowsRead: number;
  rowsImported: number;
  rowErrors: string[];
}

interface TripPaymentMatchStatistics {
  tripsConfidentMatch: number;
  tripsApproximateMatch: number;
  tripsUnmatched: number;
  tripsCancelled: number;
  paymentGroupsMatched: number;
  paymentGroupsUnmatched: number;
}

interface ImportCounts {
  parsed: number;
  added: number;
  skipped: number;
}

interface ImportSubmitResult {
  succeeded: boolean;
  error: string | null;
  driverProfileResult: CsvImportResult;
  tripsResult: CsvImportResult;
  paymentsResult: CsvImportResult;
  appAnalyticsResult: CsvImportResult;
  matchStatistics: TripPaymentMatchStatistics | null;
  driverProfileCounts: ImportCounts;
  tripsCounts: ImportCounts;
  paymentRowsCounts: ImportCounts;
  appAnalyticsEventsCounts: ImportCounts;
}

interface StepState {
  category: ImportCategory;
  label: string;
  file: File | null;
  status: StepStatus;
  result: CsvImportResult | null;
}

const API_BASE_URL = 'http://localhost:5269';

const INITIAL_STEPS: StepState[] = [
  { category: 'driver-profile', label: 'Driver Profile', file: null, status: 'idle', result: null },
  { category: 'trips', label: 'Trips', file: null, status: 'idle', result: null },
  { category: 'payments', label: 'Payments', file: null, status: 'idle', result: null },
  { category: 'app-analytics', label: 'App Analytics', file: null, status: 'idle', result: null },
];

@Component({
  selector: 'app-import-wizard',
  imports: [],
  templateUrl: './import-wizard.html',
})
export class ImportWizard {
  private readonly http = inject(HttpClient);

  protected readonly steps = signal<StepState[]>(INITIAL_STEPS.map((s) => ({ ...s })));
  protected readonly submitting = signal(false);
  protected readonly submitResult = signal<ImportSubmitResult | null>(null);
  protected readonly submitError = signal<string | null>(null);

  protected readonly allValid = computed(() => this.steps().every((s) => s.status === 'valid'));

  isUnlocked(index: number): boolean {
    return index === 0 || this.steps()[index - 1].status === 'valid';
  }

  onFileSelected(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) {
      return;
    }

    // A changed file invalidates every later step — those files are no longer
    // part of a fully-revalidated set until the wizard is walked forward again.
    this.steps.update((steps) =>
      steps.map((s, i) => {
        if (i === index) {
          return { ...s, file, status: 'validating', result: null };
        }
        return i > index ? { ...s, file: null, status: 'idle', result: null } : s;
      }),
    );

    const formData = new FormData();
    formData.append('file', file);

    const category = this.steps()[index].category;
    this.http.post<CsvImportResult>(`${API_BASE_URL}/api/imports/validate/${category}`, formData).subscribe({
      next: (result) => this.updateStep(index, { status: result.succeeded ? 'valid' : 'invalid', result }),
      error: (err: HttpErrorResponse) => {
        const result =
          err.error && typeof err.error === 'object' && 'headersFound' in err.error
            ? (err.error as CsvImportResult)
            : null;
        this.updateStep(index, { status: 'invalid', result });
      },
    });
  }

  submit(): void {
    const steps = this.steps();
    if (!steps.every((s) => s.status === 'valid' && s.file)) {
      return;
    }

    const formData = new FormData();
    formData.append('driverProfile', steps[0].file!);
    formData.append('trips', steps[1].file!);
    formData.append('payments', steps[2].file!);
    formData.append('appAnalytics', steps[3].file!);

    this.submitting.set(true);
    this.submitError.set(null);
    this.submitResult.set(null);

    this.http.post<ImportSubmitResult>(`${API_BASE_URL}/api/imports/submit`, formData).subscribe({
      next: (result) => {
        this.submitResult.set(result);
        this.submitting.set(false);
      },
      error: (err: HttpErrorResponse) => {
        const result =
          err.error && typeof err.error === 'object' && 'driverProfileResult' in err.error
            ? (err.error as ImportSubmitResult)
            : null;
        if (result) {
          this.submitResult.set(result);
        } else {
          this.submitError.set(err.message || 'Submit failed.');
        }
        this.submitting.set(false);
      },
    });
  }

  protected mappingEntries(mapping: Record<string, string>): [string, string][] {
    return Object.entries(mapping);
  }

  private updateStep(index: number, patch: Partial<StepState>): void {
    this.steps.update((steps) => steps.map((s, i) => (i === index ? { ...s, ...patch } : s)));
  }
}
