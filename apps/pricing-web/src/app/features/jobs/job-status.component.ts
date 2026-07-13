import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { JobResponse } from '../../models/api.models';
import { downloadRows, formatMoney } from '../../utilities/console-utilities';

@Component({
  selector: 'app-job-status',
  templateUrl: './job-status.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JobStatusComponent {
  readonly job = input<JobResponse | null>(null);
  readonly money = formatMoney;

  statusMark(status: string): string {
    return status === 'Completed' ? '✓' : status === 'Failed' ? '!' : '●';
  }

  download(): void {
    const currentJob = this.job();
    if (!currentJob?.results) return;
    const blob = new Blob([downloadRows(currentJob.results)], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `quote-results-${currentJob.jobId}.csv`;
    anchor.click();
    URL.revokeObjectURL(url);
  }
}
