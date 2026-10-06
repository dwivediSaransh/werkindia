import { ApolloClientOptions } from '@apollo/client/core';
import { HttpLink } from 'apollo-angular/http';
import { InMemoryCache } from '@apollo/client/cache';
import { inject } from '@angular/core';
import { authLink } from './auth-link';

export function provideIndiaWorksApollo(): ApolloClientOptions {
  const http = inject(HttpLink);
  return {
    link: authLink().concat(http.create({ uri: '/graphql' })),
    cache: new InMemoryCache(),
  };
}
