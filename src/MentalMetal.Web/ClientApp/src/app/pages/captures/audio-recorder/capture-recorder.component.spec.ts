import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { CaptureRecorderComponent } from './capture-recorder.component';

describe('CaptureRecorderComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CaptureRecorderComponent, HttpClientTestingModule],
    }).compileComponents();
  });

  it('starts in idle state with zero duration', () => {
    const fixture = TestBed.createComponent(CaptureRecorderComponent);
    const component = fixture.componentInstance as unknown as {
      state: () => string;
      durationSeconds: () => number;
      formattedDuration: () => string;
    };
    expect(component.state()).toBe('idle');
    expect(component.durationSeconds()).toBe(0);
    expect(component.formattedDuration()).toBe('00:00');
  });

  it('surfaces an error message when microphone access is denied', async () => {
    // Stub getUserMedia on the test environment.
    Object.defineProperty(navigator, 'mediaDevices', {
      configurable: true,
      value: {
        getUserMedia: () => Promise.reject(new Error('Permission denied')),
      },
    });

    const fixture = TestBed.createComponent(CaptureRecorderComponent);
    const component = fixture.componentInstance as unknown as {
      state: () => string;
      errorMessage: () => string | null;
      startRecording: () => Promise<void>;
    };

    await component.startRecording();

    expect(component.state()).toBe('error');
    expect(component.errorMessage()).toContain('Permission denied');
  });
});
