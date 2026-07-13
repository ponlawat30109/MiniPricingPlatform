import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { PricingApiClient } from '../../core/api/pricing-api.client';
import { JobSubmissionComponent } from './job-submission.component';

describe('JobSubmissionComponent', () => {
  let api: { submitJson: ReturnType<typeof vi.fn>; submitCsv: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { submitJson: vi.fn(), submitCsv: vi.fn() };
    TestBed.configureTestingModule({
      imports: [JobSubmissionComponent],
      providers: [{ provide: PricingApiClient, useValue: api }],
    }).overrideComponent(JobSubmissionComponent, { set: { template: '' } });
  });

  it('submits valid JSON and emits the accepted job id', () => {
    api.submitJson.mockReturnValue(of({ job_id: 'job-1' }));
    const component = TestBed.createComponent(JobSubmissionComponent).componentInstance;
    const accepted: string[] = [];
    component.jobAccepted.subscribe((id) => accepted.push(id));
    component.quotesJson = '[{"weight":2,"area":"Bangkok"}]';

    component.submit();

    expect(api.submitJson).toHaveBeenCalledWith([{ weight: 2, area: 'Bangkok' }]);
    expect(accepted).toEqual(['job-1']);
  });

  it('rejects invalid JSON', () => {
    const component = TestBed.createComponent(JobSubmissionComponent).componentInstance;
    component.quotesJson = '{';
    component.submit();
    expect(component.submissionError()).not.toBe('');
    expect(api.submitJson).not.toHaveBeenCalled();
  });

  it('requires a CSV file', () => {
    const component = TestBed.createComponent(JobSubmissionComponent).componentInstance;
    component.mode.set('csv');
    component.submit();
    expect(component.submissionError()).toBe('Choose a CSV file.');
  });

  it('uses a sanitized API error message', () => {
    api.submitJson.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 500,
            error: { title: 'Unable to submit job.' },
          }),
      ),
    );
    const component = TestBed.createComponent(JobSubmissionComponent).componentInstance;
    component.quotesJson = '[{"weight":2,"area":"Bangkok"}]';
    component.submit();
    expect(component.submissionError()).toBe('Unable to submit job.');
  });
});
