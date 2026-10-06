import { Injectable, signal } from '@angular/core';

import { MOCK_JOBS } from './job-data';
import { Job } from './job.model';

@Injectable({
  providedIn: 'root',
})
export class JobService {
  private readonly jobsSignal = signal<Job[]>([...MOCK_JOBS]);

  readonly jobs = this.jobsSignal.asReadonly();

  addJob(job: Job): void {
    this.jobsSignal.update((jobs) => [job, ...jobs]);
  }
}
