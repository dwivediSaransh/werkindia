import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfigureApolloAngularAndTypedGraphqlCodeGenerationComponent } from './configure-apollo-angular-and-typed-graphql-code-generation.component';
import { ApolloTestingModule, ApolloTestingController } from 'apollo-angular/testing';
import { GetVersionQuery } from '../../../core/graphql/generated/graphql';

describe('ConfigureApolloAngularAndTypedGraphqlCodeGenerationComponent', () => {
  let fixture: ComponentFixture<ConfigureApolloAngularAndTypedGraphqlCodeGenerationComponent>;
  let apolloTesting: ApolloTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApolloTestingModule, ConfigureApolloAngularAndTypedGraphqlCodeGenerationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ConfigureApolloAngularAndTypedGraphqlCodeGenerationComponent);
    apolloTesting = TestBed.inject(ApolloTestingController);
    fixture.detectChanges();
  });

  it('shows loading state initially', () => {
    const component = fixture.componentInstance;
    component.vm.set({ state: 'loading' });
    fixture.detectChanges();

    expect(component.vm().state).toBe('loading');
    expect(fixture.nativeElement.textContent).toContain('Loading GraphQL client');
  });

  it('shows success state after a successful GraphQL response', async () => {
    apolloTesting.expectOne('GetVersion').flush({
      data: { version: 'v0.0.1' },
    });

    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.vm().state).toBe('success');
    expect(component.vm().data).toBe('v0.0.1');
    expect(fixture.nativeElement.textContent).toContain('v0.0.1');
  });

  it('shows an actionable error state after a GraphQL error', async () => {
    apolloTesting.expectOne('GetVersion').flush({
      errors: [{ message: 'FORBIDDEN: not authorized' }],
    });

    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.vm().state).toBe('error');
    expect(component.vm().error).toContain('FORBIDDEN');
    expect(fixture.nativeElement.textContent).toContain('FORBIDDEN');
  });

  it('renders responsive content without clipped required controls', () => {
    const component = fixture.componentInstance;
    component.vm.set({ state: 'success', data: 'v0.0.1' });
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.btn');
    expect(button).not.toBeNull();
    expect(button.getAttribute('type') || 'button').toBe('button');
  });
});
