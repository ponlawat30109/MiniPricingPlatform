import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { PricingApiClient } from '../../core/api/pricing-api.client';
import { JobResponse } from '../../models/api.models';
import { JobsComponent } from './jobs.component';

describe('JobsComponent polling', () => {
  afterEach(() => vi.useRealTimers());

  it('cancels the in-flight poll when a new job is accepted', () => {
    const firstPoll = new Subject<JobResponse>();
    const secondPoll = new Subject<JobResponse>();
    const pricingApi = {
      job: (id: string) => (id === 'first' ? firstPoll : secondPoll),
    };
    TestBed.configureTestingModule({
      imports: [JobsComponent],
      providers: [{ provide: PricingApiClient, useValue: pricingApi }],
    }).overrideComponent(JobsComponent, { set: { template: '' } });
    const component = TestBed.createComponent(JobsComponent).componentInstance;

    component.startPolling('first');
    component.startPolling('second');

    expect(firstPoll.observed).toBe(false);
    secondPoll.next({ jobId: 'second', status: 'Completed', results: [] });
    expect(component.currentJob()?.jobId).toBe('second');
  });

  it('cancels the in-flight poll on destroy', () => {
    const poll = new Subject<JobResponse>();
    TestBed.configureTestingModule({
      imports: [JobsComponent],
      providers: [{ provide: PricingApiClient, useValue: { job: () => poll } }],
    }).overrideComponent(JobsComponent, { set: { template: '' } });
    const fixture = TestBed.createComponent(JobsComponent);

    fixture.componentInstance.startPolling('job-1');
    fixture.destroy();

    expect(poll.observed).toBe(false);
  });

  it('continues from pending through processing and stops after completion', () => {
    vi.useFakeTimers();
    const job = vi
      .fn()
      .mockReturnValueOnce(of({ jobId: 'job-1', status: 'Pending' }))
      .mockReturnValueOnce(of({ jobId: 'job-1', status: 'Processing' }))
      .mockReturnValueOnce(of({ jobId: 'job-1', status: 'Completed', results: [] }));
    TestBed.configureTestingModule({
      imports: [JobsComponent],
      providers: [{ provide: PricingApiClient, useValue: { job } }],
    }).overrideComponent(JobsComponent, { set: { template: '' } });
    const component = TestBed.createComponent(JobsComponent).componentInstance;

    component.startPolling('job-1');
    vi.advanceTimersByTime(2400);
    vi.advanceTimersByTime(2400);

    expect(job).toHaveBeenCalledTimes(3);
    expect(component.currentJob()?.status).toBe('Completed');
  });

  it('stops polling after a failed job', () => {
    vi.useFakeTimers();
    const job = vi.fn().mockReturnValue(of({ jobId: 'job-1', status: 'Failed' }));
    TestBed.configureTestingModule({
      imports: [JobsComponent],
      providers: [{ provide: PricingApiClient, useValue: { job } }],
    }).overrideComponent(JobsComponent, { set: { template: '' } });
    const component = TestBed.createComponent(JobsComponent).componentInstance;

    component.startPolling('job-1');
    vi.advanceTimersByTime(5000);

    expect(job).toHaveBeenCalledTimes(1);
  });

  it('clears a scheduled timer on destroy', () => {
    vi.useFakeTimers();
    const job = vi.fn().mockReturnValue(of({ jobId: 'job-1', status: 'Pending' }));
    TestBed.configureTestingModule({
      imports: [JobsComponent],
      providers: [{ provide: PricingApiClient, useValue: { job } }],
    }).overrideComponent(JobsComponent, { set: { template: '' } });
    const fixture = TestBed.createComponent(JobsComponent);
    fixture.componentInstance.startPolling('job-1');

    fixture.destroy();
    vi.advanceTimersByTime(5000);

    expect(job).toHaveBeenCalledTimes(1);
  });

  it('clears a prior load error when a new polling lifecycle starts', () => {
    const job = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('offline')))
      .mockReturnValueOnce(new Subject<JobResponse>());
    TestBed.configureTestingModule({
      imports: [JobsComponent],
      providers: [{ provide: PricingApiClient, useValue: { job } }],
    }).overrideComponent(JobsComponent, { set: { template: '' } });
    const component = TestBed.createComponent(JobsComponent).componentInstance;
    component.startPolling('first');
    expect(component.loadError()).not.toBe('');

    component.startPolling('second');

    expect(component.loadError()).toBe('');
  });
});
