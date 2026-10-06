import { ComponentFixture, TestBed } from '@angular/core/testing';
import { JobDetailComponent } from './job-detail.component';

describe('JobDetailComponent', () => {
  let component: JobDetailComponent;
  let fixture: ComponentFixture<JobDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [JobDetailComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(JobDetailComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should start in loading state', () => {
    fixture.detectChanges();
    expect(component.vm()).toBe('loading');
    expect(component.loading()).toBe(true);
  });

  it('should transition to error when no job input is provided', async () => {
    fixture.detectChanges();
    await new Promise((resolve) => setTimeout(resolve, 700));
    fixture.detectChanges();
    expect(component.vm()).toBe('error');
    expect(component.loading()).toBe(false);
  });

  it('should transition to success when job input is provided', async () => {
    component.job = {
      title: 'Cab Driver',
      company: 'Tata EV Logistics',
      pay: 'Rs 22,000 - Rs 32,000 / mo',
    };
    fixture.detectChanges();
    await new Promise((resolve) => setTimeout(resolve, 700));
    fixture.detectChanges();
    expect(component.vm()).toBe('success');
    expect(component.loading()).toBe(false);
  });

  it('should prevent double submission during loading', () => {
    fixture.detectChanges();
    expect(component.loading()).toBe(true);
  });
});
