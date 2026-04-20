using MentalMetal.Web.Features.Captures;

namespace MentalMetal.Web.IntegrationTests;

/// <summary>
/// Regression tests for AudioUploadOptions to prevent browser MIME type
/// rejections. Browsers produce different MIME types depending on
/// MediaRecorder implementation (Chrome vs Firefox vs Safari) and
/// whether getDisplayMedia (tab capture) is used.
/// </summary>
public class AudioUploadOptionsTests
{
    [Theory]
    [InlineData("audio/webm")]          // Chrome/Edge microphone
    [InlineData("audio/mp4")]           // Safari
    [InlineData("audio/mpeg")]          // MP3 uploads
    [InlineData("audio/wav")]           // WAV uploads
    [InlineData("audio/ogg")]           // Firefox
    [InlineData("video/webm")]          // Chrome getDisplayMedia (tab capture with video: true)
    public void DefaultAllowedMimeTypes_IncludesBrowserVariant(string mimeType)
    {
        var options = new AudioUploadOptions();

        Assert.Contains(mimeType, options.AllowedMimeTypes, StringComparer.OrdinalIgnoreCase);
    }
}
