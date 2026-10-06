import { Component, signal } from '@angular/core';

interface ViewModel {
  state: 'loading' | 'success' | 'error';
  data?: string;
  error?: string;
}

@Component({
  selector: 'app-create-angular-standalone-workspace',
  standalone: true,
  imports: [],
  templateUrl: './create-angular-standalone-workspace.component.html',
  styleUrls: ['./create-angular-standalone-workspace.component.scss'],
})
export class CreateAngularStandaloneWorkspaceComponent {
  readonly vm = signal<ViewModel>({ state: 'loading' });

  constructor() {
    // Simulate API call
    setTimeout(() => {
      this.vm.set({ state: 'success', data: 'Workspace loaded successfully' });
    }, 1000);
  }

  reload() {
    this.vm.set({ state: 'loading' });
    setTimeout(() => {
      // Simulate random success or error
      if (Math.random() > 0.5) {
        this.vm.set({ state: 'success', data: 'Workspace loaded successfully' });
      } else {
        this.vm.set({ state: 'error', error: 'Something went wrong' });
      }
    }, 1000);
  }
}
