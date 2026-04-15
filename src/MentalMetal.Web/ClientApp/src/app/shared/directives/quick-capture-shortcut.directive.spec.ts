import { Component, inject } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { QuickCaptureShortcutDirective } from './quick-capture-shortcut.directive';
import { QuickCaptureUiService } from '../services/quick-capture-ui.service';

@Component({
  standalone: true,
  imports: [QuickCaptureShortcutDirective],
  template: `<div appQuickCaptureShortcut></div>`,
})
class HostComponent {
  readonly ui = inject(QuickCaptureUiService);
}

/**
 * Set navigator.platform for the duration of a test. Returns a restore fn.
 * macOS detection in the directive reads navigator.platform / userAgent.
 */
function setPlatform(platform: string): () => void {
  const original = Object.getOwnPropertyDescriptor(navigator, 'platform');
  Object.defineProperty(navigator, 'platform', {
    configurable: true,
    get: () => platform,
  });
  return () => {
    if (original) Object.defineProperty(navigator, 'platform', original);
    else delete (navigator as unknown as Record<string, unknown>)['platform'];
  };
}

describe('QuickCaptureShortcutDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let ui: QuickCaptureUiService;
  let restorePlatform: () => void = () => undefined;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    ui = fixture.componentInstance.ui;
  });

  afterEach(() => {
    restorePlatform();
    restorePlatform = () => undefined;
    vi.restoreAllMocks();
  });

  function press(init: KeyboardEventInit): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { bubbles: true, cancelable: true, ...init });
    window.dispatchEvent(event);
    return event;
  }

  it('opens on Ctrl+K on non-mac platforms', () => {
    restorePlatform = setPlatform('Win32');
    const openSpy = vi.spyOn(ui, 'open');

    const event = press({ key: 'k', ctrlKey: true });

    expect(openSpy).toHaveBeenCalled();
    expect(event.defaultPrevented).toBe(true);
  });

  it('opens on Cmd+K on macOS', () => {
    restorePlatform = setPlatform('MacIntel');
    const openSpy = vi.spyOn(ui, 'open');

    press({ key: 'k', metaKey: true });
    expect(openSpy).toHaveBeenCalled();
  });

  it('ignores Cmd+K on non-mac (where Ctrl is the primary modifier)', () => {
    restorePlatform = setPlatform('Win32');
    const openSpy = vi.spyOn(ui, 'open');

    press({ key: 'k', metaKey: true });
    expect(openSpy).not.toHaveBeenCalled();
  });

  it('ignores plain K with no modifier', () => {
    restorePlatform = setPlatform('Win32');
    const openSpy = vi.spyOn(ui, 'open');

    press({ key: 'k' });
    expect(openSpy).not.toHaveBeenCalled();
  });

  it('ignores Ctrl+Shift+K so browser-specific shortcuts are not stolen', () => {
    restorePlatform = setPlatform('Win32');
    const openSpy = vi.spyOn(ui, 'open');

    press({ key: 'k', ctrlKey: true, shiftKey: true });
    expect(openSpy).not.toHaveBeenCalled();
  });
});
