import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { PricingApiClient } from '../../core/api/pricing-api.client';
import { httpErrorMessage } from '../../core/http/problem-details';
import { JobResponse } from '../../models/api.models';
import { Subscription } from 'rxjs';
import { JobStatusComponent } from './job-status.component';
import { JobSubmissionComponent } from './job-submission.component';

@Component({
  selector: 'app-jobs',
  imports: [JobSubmissionComponent, JobStatusComponent],
  templateUrl: './jobs.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JobsComponent implements OnDestroy {
  private readonly pricingApi = inject(PricingApiClient);
  private pollTimer?: ReturnType<typeof setTimeout>;
  private pollSubscription?: Subscription;
  private pollGeneration = 0;
  private destroyed = false;

  readonly currentJob = signal<JobResponse | null>(null);
  readonly loadError = signal('');

  startPolling(jobId: string): void {
    this.cancelPolling();
    this.loadError.set('');
    this.destroyed = false;
    const generation = ++this.pollGeneration;
    this.poll(jobId, generation);
  }

  private poll(jobId: string, generation: number): void {
    this.pollSubscription = this.pricingApi.job(jobId).subscribe({
      next: (job) => {
        if (this.destroyed || generation !== this.pollGeneration) return;
        this.currentJob.set(job);
        if (job.status === 'Pending' || job.status === 'Processing') {
          this.pollTimer = setTimeout(() => this.poll(jobId, generation), 1200);
        }
      },
      error: (error) => {
        if (!this.destroyed && generation === this.pollGeneration) {
          this.loadError.set(httpErrorMessage(error));
        }
      },
    });
  }

  private cancelPolling(): void {
    if (this.pollTimer) clearTimeout(this.pollTimer);
    this.pollTimer = undefined;
    this.pollSubscription?.unsubscribe();
    this.pollSubscription = undefined;
    this.pollGeneration++;
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.cancelPolling();
  }
}
