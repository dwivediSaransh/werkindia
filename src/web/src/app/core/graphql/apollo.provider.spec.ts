import { TestBed } from '@angular/core/testing';
import { HttpLink } from 'apollo-angular/http';
import { provideIndiaWorksApollo } from './apollo.provider';

describe('provideIndiaWorksApollo', () => {
  it('should return an Apollo client options object with a GraphQL link and in-memory cache', () => {
    // Arrange
    let capturedUri: string | undefined;
    const mockHttpLink = {
      create: ({ uri }: { uri: string }) => {
        capturedUri = uri;
        return { concat: () => ({ subscribe: () => ({}) }) };
      },
    };

    TestBed.configureTestingModule({
      providers: [{ provide: HttpLink, useValue: mockHttpLink }],
    });

    // Act — call the provider factory inside an injection context
    const result = TestBed.runInInjectionContext(() => {
      return provideIndiaWorksApollo();
    });

    // Assert
    expect(result).toBeDefined();
    expect(result.link).toBeTruthy();
    expect(result.cache).toBeTruthy();
    expect(capturedUri).toBe('/graphql');
  });
});
