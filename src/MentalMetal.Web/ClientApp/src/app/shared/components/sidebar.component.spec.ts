import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, beforeAll } from 'vitest';
import { SidebarComponent } from './sidebar.component';

describe('SidebarComponent', () => {
  let fixture: ComponentFixture<SidebarComponent>;
  let router: Router;

  beforeAll(() => {
    // ThemeService reads window.matchMedia at construction time; the
    // happy-dom test environment doesn't implement it. Define as
    // writable/configurable so other suites can spy on / stub it.
    if (!window.matchMedia) {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        configurable: true,
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

  /** Visible text only — ignores HTML comments / attribute values. */
  function text(): string {
    return (fixture.nativeElement.textContent ?? '') as string;
  }
  function workToggle(): HTMLButtonElement {
    return fixture.nativeElement.querySelector(
      'button[aria-controls="work-group"]',
    ) as HTMLButtonElement;
  }
  function moreToggle(): HTMLButtonElement {
    return fixture.nativeElement.querySelector(
      'button[aria-controls="more-group"]',
    ) as HTMLButtonElement;
  }
  function hasChild(label: string): boolean {
    const links = Array.from(
      fixture.nativeElement.querySelectorAll('nav a') as NodeListOf<HTMLElement>,
    );
    return links.some((a) => (a.textContent ?? '').trim() === label);
  }

  it('renders the six primary verbs in visible text', () => {
    const t = text();
    for (const label of ['Today', 'Chat', 'Capture', 'People', 'Initiatives', 'Work']) {
      expect(t).toContain(label);
    }
  });

  it('toggles aria-expanded and reveals Work children when expanded', () => {
    expect(hasChild('My Queue')).toBe(false);
    expect(workToggle().getAttribute('aria-expanded')).toBe('false');

    workToggle().click();
    fixture.detectChanges();

    expect(workToggle().getAttribute('aria-expanded')).toBe('true');
    for (const label of ['My Queue', 'Commitments', 'Delegations', 'Nudges', 'Close-out']) {
      expect(hasChild(label)).toBe(true);
    }
  });

  it('toggles aria-expanded and reveals More children when expanded', () => {
    expect(hasChild('Observations')).toBe(false);
    expect(moreToggle().getAttribute('aria-expanded')).toBe('false');

    moreToggle().click();
    fixture.detectChanges();

    expect(moreToggle().getAttribute('aria-expanded')).toBe('true');
    for (const label of ['1:1s', 'Observations', 'Goals', 'Interviews', 'Weekly Briefing']) {
      expect(hasChild(label)).toBe(true);
    }
  });

  it('auto-expands the Work group when navigating to a Work child route', async () => {
    await router.navigateByUrl('/commitments');
    fixture.detectChanges();
    fixture.detectChanges();

    expect(workToggle().getAttribute('aria-expanded')).toBe('true');
    expect(hasChild('Commitments')).toBe(true);
    expect(hasChild('Observations')).toBe(false);
  });

  it('auto-expands the More group when navigating with a query string', async () => {
    await router.navigateByUrl('/observations?personId=abc');
    fixture.detectChanges();
    fixture.detectChanges();

    expect(moreToggle().getAttribute('aria-expanded')).toBe('true');
    expect(hasChild('Observations')).toBe(true);
    expect(hasChild('My Queue')).toBe(false);
  });
});
