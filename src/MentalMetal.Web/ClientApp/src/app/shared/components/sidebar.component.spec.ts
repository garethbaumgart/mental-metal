import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, beforeAll } from 'vitest';
import { SidebarComponent } from './sidebar.component';

describe('SidebarComponent', () => {
  let fixture: ComponentFixture<SidebarComponent>;
  let router: Router;

  beforeAll(() => {
    // ThemeService reads window.matchMedia at construction time; the
    // happy-dom test environment doesn't implement it.
    if (!window.matchMedia) {
      Object.defineProperty(window, 'matchMedia', {
        value: (query: string) => ({
          matches: false,
          media: query,
          addEventListener: () => undefined,
          removeEventListener: () => undefined,
          addListener: () => undefined,
          removeListener: () => undefined,
          dispatchEvent: () => false,
          onchange: null,
        }),
      });
    }
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [provideRouter([{ path: '**', children: [] }])],
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  function html(): string {
    return fixture.nativeElement.innerHTML as string;
  }

  it('renders the six primary verbs', () => {
    const primary = html();
    expect(primary).toContain('Today');
    expect(primary).toContain('Chat');
    expect(primary).toContain('Capture');
    expect(primary).toContain('People');
    expect(primary).toContain('Initiatives');
    expect(primary).toContain('Work');
  });

  it('hides Work-group children until the group is expanded', () => {
    expect(html()).not.toContain('My Queue');

    const workToggle = fixture.nativeElement.querySelector(
      'button[aria-controls="work-group"]',
    ) as HTMLButtonElement;
    expect(workToggle).toBeTruthy();
    workToggle.click();
    fixture.detectChanges();

    expect(html()).toContain('My Queue');
    expect(html()).toContain('Commitments');
    expect(html()).toContain('Delegations');
    expect(html()).toContain('Nudges');
    expect(html()).toContain('Close-out');
  });

  it('hides More-group children until the group is expanded', () => {
    expect(html()).not.toContain('Observations');

    const moreToggle = fixture.nativeElement.querySelector(
      'button[aria-controls="more-group"]',
    ) as HTMLButtonElement;
    moreToggle.click();
    fixture.detectChanges();

    expect(html()).toContain('1:1s');
    expect(html()).toContain('Observations');
    expect(html()).toContain('Goals');
    expect(html()).toContain('Interviews');
    expect(html()).toContain('Weekly Briefing');
  });

  it('auto-expands the Work group when navigating to a Work child route', async () => {
    await router.navigateByUrl('/commitments');
    fixture.detectChanges();
    // Allow the effect to run
    fixture.detectChanges();

    expect(html()).toContain('My Queue');
    expect(html()).toContain('Commitments');
    expect(html()).not.toContain('Observations');
  });

  it('auto-expands the More group when navigating to a More child route', async () => {
    await router.navigateByUrl('/observations');
    fixture.detectChanges();
    fixture.detectChanges();

    expect(html()).toContain('Observations');
    expect(html()).toContain('Goals');
    expect(html()).not.toContain('My Queue');
  });
});
