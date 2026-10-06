import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreateAngularStandaloneWorkspaceComponent } from './create-angular-standalone-workspace.component';

describe('CreateAngularStandaloneWorkspaceComponent', () => {
  let component: CreateAngularStandaloneWorkspaceComponent;
  let fixture: ComponentFixture<CreateAngularStandaloneWorkspaceComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateAngularStandaloneWorkspaceComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateAngularStandaloneWorkspaceComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should start in loading state', async () => {
    expect(component.vm().state).toBe('loading');
    await new Promise((resolve) => setTimeout(resolve, 1000));
    expect(component.vm().state).toBe('success');
  });

  it('should show error when reload fails', async () => {
    // Spy on Math.random
    const originalRandom = Math.random;
    Math.random = () => 0.4; // will cause error (<0.5)
    try {
      component.reload();
      await new Promise((resolve) => setTimeout(resolve, 1000));
      expect(component.vm().state).toBe('error');
    } finally {
      Math.random = originalRandom;
    }
  });
});
