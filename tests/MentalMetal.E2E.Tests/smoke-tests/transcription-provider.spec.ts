import { test, expect, API_BASE } from './fixtures/auth.fixture';

test.describe('Transcription Provider Settings', () => {
  test('Deepgram transcription section is visible on settings page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/settings');

    await expect(authenticatedPage).toHaveURL(/\/settings/);
    await expect(authenticatedPage.getByRole('heading', { name: 'Deepgram Transcription' })).toBeVisible();
  });

  test('Provider status returns isConfigured=false for new user', async ({ authenticatedPage, testUser }) => {
    const response = await authenticatedPage.request.get(`${API_BASE}/api/users/me/transcription-provider`, {
      headers: { Authorization: `Bearer ${testUser.accessToken}` },
    });

    expect(response.ok()).toBeTruthy();
    const status = await response.json();
    expect(status.isConfigured).toBe(false);
    expect(status.provider).toBeNull();
    expect(status.model).toBeNull();
  });

  test('Validate endpoint rejects invalid API key', async ({ authenticatedPage, testUser }) => {
    const response = await authenticatedPage.request.post(
      `${API_BASE}/api/users/me/transcription-provider/validate`,
      {
        headers: { Authorization: `Bearer ${testUser.accessToken}` },
        data: { apiKey: 'invalid-key-12345' },
      },
    );

    expect(response.ok()).toBeTruthy();
    const result = await response.json();
    expect(result.success).toBe(false);
    expect(result.error).toBeTruthy();
  });

  test('Audio upload without configured provider returns transcription.notConfigured', async ({
    authenticatedPage,
    testUser,
  }) => {
    // Create a minimal audio file (WAV header)
    const wavHeader = new Uint8Array([
      0x52, 0x49, 0x46, 0x46, // "RIFF"
      0x24, 0x00, 0x00, 0x00, // file size
      0x57, 0x41, 0x56, 0x45, // "WAVE"
      0x66, 0x6d, 0x74, 0x20, // "fmt "
      0x10, 0x00, 0x00, 0x00, // chunk size
      0x01, 0x00,             // PCM
      0x01, 0x00,             // mono
      0x80, 0x3e, 0x00, 0x00, // 16000 Hz
      0x00, 0x7d, 0x00, 0x00, // byte rate
      0x02, 0x00,             // block align
      0x10, 0x00,             // bits per sample
      0x64, 0x61, 0x74, 0x61, // "data"
      0x00, 0x00, 0x00, 0x00, // data size
    ]);

    const response = await authenticatedPage.request.post(`${API_BASE}/api/captures/audio`, {
      headers: { Authorization: `Bearer ${testUser.accessToken}` },
      multipart: {
        file: {
          name: 'test.wav',
          mimeType: 'audio/wav',
          buffer: Buffer.from(wavHeader),
        },
        durationSeconds: '1.0',
      },
    });

    expect(response.ok()).toBeFalsy();
    const body = await response.json();
    expect(body.code).toBe('transcription.notConfigured');
  });
});
