import { Component, input, output } from '@angular/core';

import { Job } from './job.model';

@Component({
  selector: 'app-job-card',
  template: `
    <article class="iw-job-card" [class.iw-job-card--saved]="isSaved()">
      <button type="button" class="job-card__main" (click)="selectJob.emit(job())">
        <span class="job-card__logo" aria-hidden="true">{{ job().company.logoText }}</span>

        <span class="job-card__content">
          <span class="job-card__meta">
            <strong>{{ job().company.name }}</strong>
            <span>{{ job().jobType }}</span>
            <span>{{ job().openingsCount }} openings</span>
            <span>{{ job().postedDate }}</span>
          </span>
          <span class="job-card__title">{{ job().title }}</span>
          <span class="job-card__summary">{{ job().summary }}</span>
          <span class="job-card__details">
            <span>{{ job().locality }}, {{ job().city }}</span>
            <span>{{ job().workType }}</span>
            <span>{{ job().shiftType }}</span>
            <span>{{ job().experienceRequired }}</span>
          </span>
          <span class="job-card__tags">
            @for (badge of job().complianceBadges.slice(0, 3); track badge) {
              <span>{{ badge }}</span>
            }
          </span>
        </span>
      </button>

      <div class="job-card__aside">
        @if (job().urgency === 'Immediate Joining (24h)') {
          <span class="job-card__urgent">24h joining</span>
        }
        <span class="job-card__salary">{{ job().displayPay }}</span>
        <span class="job-card__pay-note">{{ job().payoutCadence }} payout</span>
        <div class="job-card__actions">
          <button
            type="button"
            class="icon-button"
            [attr.aria-pressed]="isSaved()"
            (click)="toggleSave.emit(job().id)"
          >
            <span aria-hidden="true">{{ isSaved() ? 'Saved' : 'Save' }}</span>
            <span class="sr-only">{{ isSaved() ? 'Remove saved job' : 'Save job' }}</span>
          </button>
          <button type="button" class="secondary-action" (click)="selectJob.emit(job())">
            Details
          </button>
          <button type="button" class="primary-action" (click)="quickApply.emit(job())">
            Apply
          </button>
        </div>
      </div>
    </article>
  `,
  styleUrl: './job-card.component.scss',
})
export class JobCardComponent {
  readonly job = input.required<Job>();
  readonly isSaved = input(false);

  readonly selectJob = output<Job>();
  readonly toggleSave = output<string>();
  readonly quickApply = output<Job>();
}
