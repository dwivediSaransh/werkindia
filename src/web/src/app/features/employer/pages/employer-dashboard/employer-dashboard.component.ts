import { Component, signal } from '@angular/core';

@Component({
  selector: 'iw-employer-dashboard',
  standalone: true,
  templateUrl: './employer-dashboard.component.html',
  styleUrl: './employer-dashboard.component.scss',
})
export class EmployerDashboardComponent {
  readonly vm = signal<'loading' | 'success' | 'empty'>('loading');

  ngOnInit(): void {
    setTimeout(() => {
      this.vm.set('success');
    }, 500);
  }

  activeTab = signal<'jobs' | 'applicants' | 'settings'>('jobs');
}
