import { Component, signal } from '@angular/core';

@Component({
  selector: 'iw-admin-dashboard',
  standalone: true,
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
})
export class AdminDashboardComponent {
  readonly vm = signal<'loading' | 'success'>('loading');

  ngOnInit(): void {
    setTimeout(() => {
      this.vm.set('success');
    }, 500);
  }
}
