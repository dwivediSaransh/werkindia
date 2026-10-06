import { Component, Input, signal } from '@angular/core';

@Component({
  selector: 'iw-job-detail',
  standalone: true,
  imports: [],
  templateUrl: './job-detail.component.html',
  styleUrl: './job-detail.component.scss',
})
export class JobDetailComponent {
  readonly loading = signal(false);
  @Input() job: { title: string; company: string; pay: string } | null = null;

  readonly vm = signal<'loading' | 'error' | 'success'>('loading');

  ngOnInit(): void {
    this.loading.set(true);
    setTimeout(() => {
      this.loading.set(false);
      this.vm.set(this.job ? 'success' : 'error');
    }, 600);
  }
}
