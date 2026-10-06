import { TestBed } from '@angular/core/testing';
import { ApolloTestingModule } from 'apollo-angular/testing';
import { AuthSessionStore } from '../auth/auth-session.store';
import { authLink } from './auth-link';
import { ApolloLink } from '@apollo/client';
import { Observable } from 'rxjs';

function waitForContextLink(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

describe('authLink', () => {
  let store: AuthSessionStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ApolloTestingModule],
      providers: [AuthSessionStore],
    });
    store = TestBed.inject(AuthSessionStore);
  });

  it('passes request through when no token is set', async () => {
    store.clearSession();
    const link = TestBed.runInInjectionContext(() => authLink());

    let context: Record<string, unknown> = {};
    let capturedHeaders: Record<string, string> | undefined;
    const forward: ApolloLink.ForwardFunction = (operation) => {
      capturedHeaders = operation.getContext()['headers'] as Record<string, string> | undefined;
      return new Observable<ApolloLink.Result>((subscriber) => subscriber.complete());
    };

    link
      .request(
        {
          operationName: 'Test',
          query: { kind: 'Document', definitions: [] } as never,
          variables: {},
          getContext: () => context,
          setContext: (next: Record<string, unknown>) => {
            context = { ...context, ...next };
          },
        } as never,
        forward,
      )
      .subscribe();

    await waitForContextLink();
    expect(capturedHeaders).toBeUndefined();
  });

  it('attaches Authorization header when a token is set', async () => {
    store.setSession({
      accessToken: 'test-token-value',
      refreshTokenHash: null,
      userId: null,
      deviceId: null,
      sessionId: null,
      persona: null,
      expiresOn: null,
    });
    const link = TestBed.runInInjectionContext(() => authLink());

    let context: Record<string, unknown> = {};
    let capturedHeaders: Record<string, string> | undefined;
    const forward: ApolloLink.ForwardFunction = (operation) => {
      capturedHeaders = operation.getContext()['headers'] as Record<string, string> | undefined;
      return new Observable<ApolloLink.Result>((subscriber) => subscriber.complete());
    };

    link
      .request(
        {
          operationName: 'Test',
          query: { kind: 'Document', definitions: [] } as never,
          variables: {},
          getContext: () => context,
          setContext: (next: Record<string, unknown>) => {
            context = { ...context, ...next };
          },
        } as never,
        forward,
      )
      .subscribe();

    await waitForContextLink();
    expect(capturedHeaders?.['Authorization']).toBe('Bearer test-token-value');
  });

  it('does not log token values or secrets', async () => {
    store.setSession({
      accessToken: 'super-secret-jwt',
      refreshTokenHash: null,
      userId: null,
      deviceId: null,
      sessionId: null,
      persona: null,
      expiresOn: null,
    });
    const link = TestBed.runInInjectionContext(() => authLink());

    let context: Record<string, unknown> = {};
    const emitted: unknown[] = [];
    const forward: ApolloLink.ForwardFunction = (op) => {
      emitted.push(op.getContext());
      return new Observable<ApolloLink.Result>((subscriber) => subscriber.complete());
    };

    link
      .request(
        {
          operationName: 'Test',
          query: { kind: 'Document', definitions: [] } as never,
          variables: {},
          getContext: () => context,
          setContext: (next: Record<string, unknown>) => {
            context = { ...context, ...next };
          },
        } as never,
        forward,
      )
      .subscribe();

    await waitForContextLink();
    const headers = (emitted[0] as { headers?: Record<string, string> }).headers;
    expect(headers?.['Authorization']).toBe('Bearer super-secret-jwt');
  });
});
