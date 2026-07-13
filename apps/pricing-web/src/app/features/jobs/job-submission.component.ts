import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PricingApiClient } from '../../core/api/pricing-api.client';
import { httpErrorMessage } from '../../core/http/problem-details';
import { QuoteRequest } from '../../models/api.models';

@Component({
  selector: 'app-job-submission',
  imports: [FormsModule],
  templateUrl: './job-submission.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JobSubmissionComponent {
  private readonly pricingApi = inject(PricingApiClient);

  readonly jobAccepted = output<string>();
  readonly mode = signal<'json' | 'csv'>('json');
  readonly selectedFile = signal<File | null>(null);
  readonly submitting = signal(false);
  readonly submissionError = signal('');
  quotesJson =
    '[\n  { "weight": 2.5, "area": "Bangkok" },\n' +
    '  { "weight": 8, "area": "Other Provinces" }\n]';

  selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  submit(): void {
    this.submissionError.set('');
    this.submitting.set(true);
    try {
      const request = this.mode() === 'json' ? this.submitJson() : this.submitCsv();
      request.subscribe({
        next: (response) => {
          this.submitting.set(false);
          this.jobAccepted.emit(response.job_id);
        },
        error: (error) => {
          this.submissionError.set(httpErrorMessage(error));
          this.submitting.set(false);
        },
      });
    } catch (error) {
      this.submissionError.set(error instanceof Error ? error.message : 'Invalid input.');
      this.submitting.set(false);
    }
  }

  private submitJson() {
    const quotes = JSON.parse(this.quotesJson) as QuoteRequest[];
    if (!Array.isArray(quotes) || !quotes.length) throw new Error('Enter at least one quote.');
    return this.pricingApi.submitJson(quotes);
  }

  private submitCsv() {
    const file = this.selectedFile();
    if (!file) throw new Error('Choose a CSV file.');
    return this.pricingApi.submitCsv(file);
  }
}
