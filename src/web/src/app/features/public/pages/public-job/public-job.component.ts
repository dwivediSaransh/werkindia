import { Component, signal } from '@angular/core';

@Component({
  selector: 'iw-public-job',
  standalone: true,
  templateUrl: './public-job.component.html',
  styleUrl: './public-job.component.scss',
})
export class PublicJobComponent {
  readonly loading = signal(false);
  readonly vm = signal<'loading' | 'success' | 'expired'>('loading');

  ngOnInit(): void {
    this.loading.set(true);
    setTimeout(() => {
      this.loading.set(false);
      this.vm.set('success');
    }, 600);
  }
}
