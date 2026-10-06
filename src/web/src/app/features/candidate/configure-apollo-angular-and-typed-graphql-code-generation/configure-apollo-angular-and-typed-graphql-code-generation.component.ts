import { Component, OnInit, signal, ChangeDetectionStrategy, inject } from '@angular/core';
import { Apollo } from 'apollo-angular';
import {
  GetVersionDocument,
  GetVersionQuery,
  GetVersionQueryVariables,
} from '../../../core/graphql/generated/graphql';

interface ViewModel {
  state: 'idle' | 'loading' | 'success' | 'error';
  data?: string;
  error?: string;
}

@Component({
  selector: 'app-configure-apollo-angular-and-typed-graphql-code-generation',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './configure-apollo-angular-and-typed-graphql-code-generation.component.html',
  styleUrl: './configure-apollo-angular-and-typed-graphql-code-generation.component.scss',
})
export class ConfigureApolloAngularAndTypedGraphqlCodeGenerationComponent implements OnInit {
  private readonly apollo = inject(Apollo);
  readonly vm = signal<ViewModel>({ state: 'idle' });

  ngOnInit(): void {
    this.fetchVersion();
  }

  fetchVersion(): void {
    if (this.vm().state === 'loading') {
      return;
    }

    this.vm.set({ state: 'loading' });

    this.apollo
      .watchQuery<GetVersionQuery, GetVersionQueryVariables>({
        query: GetVersionDocument,
        fetchPolicy: 'network-only',
      })
      .valueChanges.subscribe({
        next: ({ data, error }) => {
          if (error) {
            const message =
              (error as { message?: string }).message ?? 'Unable to reach the GraphQL endpoint.';
            this.vm.set({ state: 'error', error: message });
            return;
          }
          if (data?.['version']) {
            this.vm.set({ state: 'success', data: data['version'] as string });
          } else {
            this.vm.set({
              state: 'error',
              error: 'Unexpected response from the GraphQL endpoint.',
            });
          }
        },
        error: () => {
          this.vm.set({ state: 'error', error: 'Unable to reach the GraphQL endpoint.' });
        },
      });
  }
}
