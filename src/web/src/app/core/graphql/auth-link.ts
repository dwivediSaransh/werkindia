import { ApolloLink } from '@apollo/client';
import { SetContextLink } from '@apollo/client/link/context';
import { inject } from '@angular/core';
import { AuthSessionStore } from '../auth/auth-session.store';

export function authLink(): ApolloLink {
  const store = inject(AuthSessionStore);
  return new SetContextLink((_prevContext, _operation) => {
    const token = store.accessToken();
    if (!token) {
      return {};
    }
    return { headers: { Authorization: 'Bearer ' + token } };
  });
}
