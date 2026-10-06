import { TestBed } from '@angular/core/testing';
import { RouterOutlet } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { NotFoundComponent } from './shared/pages/not-found/not-found.component';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App, RouterOutlet, NotFoundComponent],
      providers: [{ provide: 'ROUTES', useValue: routes }],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the application outlet', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });

  it('should define candidate, employer, admin and public lazy route groups', () => {
    const allPaths = routes.map((route) => route.path).filter((p) => p !== '**');
    expect(allPaths).toContain('candidate');
    expect(allPaths).toContain('employer');
    expect(allPaths).toContain('admin');
    expect(allPaths).toContain('share');
    expect(routes.find((route) => route.path === '**')?.component).toBe(NotFoundComponent);
  });
});
