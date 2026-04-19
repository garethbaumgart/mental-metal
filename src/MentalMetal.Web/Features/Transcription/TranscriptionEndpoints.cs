using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace MentalMetal.Web.Features.Transcription;

public static class TranscriptionEndpoints
{
    private const int BufferSize = 16384;

    public static void MapTranscriptionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/transcription")
            .RequireAuthorization();

        group.MapGet("/status", HandleStatus);

        // WebSocket endpoint — cookie-based auth works automatically since cookies
        // are sent with WS upgrade requests. Mapped outside the auth group because
        // JavaScript's WebSocket API cannot send custom Authorization headers.
        routes.Map("/api/transcription/stream", HandleStream);
    }

    private static async Task<IResult> HandleStatus(
        IOptions<DeepgramSettings> settings,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IApiKeyEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("TranscriptionEndpoints");

        var user = await userRepository.GetByIdAsync(
            currentUserService.UserId, cancellationToken);
        if (user is null)
        {
            return Results.Ok(new
            {
                available = false,
                reason = "User not found.",
            });
        }

        var config = user.TranscriptionProviderConfig;
        if (config is null)
        {
            return Results.Ok(new
            {
                available = false,
                reason = "Transcription provider not configured. Add your Deepgram API key in Settings.",
            });
        }

        var apiKey = encryptionService.Decrypt(config.EncryptedApiKey);

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token, cancellationToken);
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Token", apiKey);

            var baseUrl = settings.Value.BaseUrl;
            var httpScheme = IsLoopbackAddress(baseUrl) ? "http" : "https";
            using var response = await httpClient.GetAsync(
                $"{httpScheme}://{baseUrl}/v1/projects", linkedCts.Token);

            if (response.IsSuccessStatusCode)
            {
                return Results.Ok(new { available = true });
            }

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                or System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogWarning("Deepgram API key is invalid (HTTP {StatusCode})",
                    (int)response.StatusCode);
                return Results.Ok(new
                {
                    available = false,
                    reason = "Transcription API key is invalid.",
                });
            }

            logger.LogWarning("Deepgram connectivity check failed (HTTP {StatusCode})",
                (int)response.StatusCode);
            return Results.Ok(new
            {
                available = false,
                reason = "Transcription service returned an error. Please try again.",
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Deepgram connectivity check timed out");
            return Results.Ok(new
            {
                available = false,
                reason = "Transcription service is not responding. Please try again.",
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Deepgram connectivity check failed");
            return Results.Ok(new
            {
                available = false,
                reason = "Cannot reach transcription service. Please check your connection.",
            });
        }
    }

    private static async Task HandleStream(
        HttpContext context,
        IOptions<DeepgramSettings> settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TranscriptionEndpoints");
        var sessionId = Guid.NewGuid().ToString("N")[..8];

        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("[{SessionId}] Transcription WebSocket rejected: user not authenticated", sessionId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Validate Origin header to prevent cross-site WebSocket hijacking
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
        {
            var requestHost = context.Request.Host.ToString();
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
                || !string.Equals(originUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("[{SessionId}] Transcription WebSocket rejected: Origin '{Origin}' does not match host '{Host}'",
                    sessionId, origin, requestHost);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        // Resolve the user's Deepgram API key from their TranscriptionProviderConfig
        var userRepository = context.RequestServices.GetRequiredService<IUserRepository>();
        var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
        var encryptionService = context.RequestServices.GetRequiredService<IApiKeyEncryptionService>();

        var appUser = await userRepository.GetByIdAsync(
            currentUserService.UserId, context.RequestAborted);

        if (appUser?.TranscriptionProviderConfig is null)
        {
            logger.LogWarning("[{SessionId}] Transcription WebSocket rejected: no transcription provider configured", sessionId);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Transcription provider not configured. Add your Deepgram API key in Settings.");
            return;
        }

        var deepgramApiKey = encryptionService.Decrypt(appUser.TranscriptionProviderConfig.EncryptedApiKey);
        var deepgramSettings = settings.Value;

        using var clientWs = await context.WebSockets.AcceptWebSocketAsync();
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        logger.LogInformation("[{SessionId}] Transcription session started for user {UserId}", sessionId, userId);

        // Build Deepgram streaming URL
        var channels = context.Request.Query["channels"].FirstOrDefault();
        var mimeType = context.Request.Query["mimeType"].FirstOrDefault();
        var encoding = context.Request.Query["encoding"].FirstOrDefault();
        var sampleRate = context.Request.Query["sampleRate"].FirstOrDefault();
        var isMultichannel = int.TryParse(channels, out var channelCount) && channelCount > 1;
        var hasExplicitEncoding = !string.IsNullOrEmpty(encoding);

        if (!hasExplicitEncoding)
        {
            if (isMultichannel && IsContainerFormat(mimeType))
            {
                logger.LogInformation(
                    "[{SessionId}] Multichannel requested with container format (mimeType={MimeType}), " +
                    "falling back to single-channel with diarization",
                    sessionId, mimeType ?? "auto-detect");
                isMultichannel = false;
            }
        }

        // Use the user's configured model for the streaming session
        var streamModel = appUser.TranscriptionProviderConfig.Model;

        var queryParams = new List<string>
        {
            $"model={Uri.EscapeDataString(streamModel)}",
            $"punctuate={deepgramSettings.Punctuate.ToString().ToLowerInvariant()}",
            $"interim_results={deepgramSettings.InterimResults.ToString().ToLowerInvariant()}",
            $"language={Uri.EscapeDataString(deepgramSettings.Language)}",
            "utterance_end_ms=1000",
            $"diarize={deepgramSettings.Diarize.ToString().ToLowerInvariant()}",
        };

        if (isMultichannel)
        {
            queryParams.Add("multichannel=true");
            queryParams.Add($"channels={channelCount}");
        }

        if (hasExplicitEncoding)
        {
            queryParams.Add($"encoding={Uri.EscapeDataString(encoding!)}");
            if (!string.IsNullOrEmpty(sampleRate))
            {
                queryParams.Add($"sample_rate={Uri.EscapeDataString(sampleRate)}");
            }
        }
        else if (!string.IsNullOrEmpty(mimeType))
        {
            if (!IsContainerFormat(mimeType, treatEmptyAsContainer: false))
            {
                queryParams.Add($"encoding={Uri.EscapeDataString(mimeType)}");
            }
        }

        var wsScheme = IsLoopbackAddress(deepgramSettings.BaseUrl) ? "ws" : "wss";
        var deepgramUrl = $"{wsScheme}://{deepgramSettings.BaseUrl}/v1/listen?{string.Join("&", queryParams)}";

        using var deepgramWs = new ClientWebSocket();
        deepgramWs.Options.SetRequestHeader("Authorization", $"Token {deepgramApiKey}");
        deepgramWs.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

        try
        {
            using var connectTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var connectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                connectTimeoutCts.Token, context.RequestAborted);
            await deepgramWs.ConnectAsync(new Uri(deepgramUrl), connectLinkedCts.Token);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("[{SessionId}] Client disconnected during Deepgram WebSocket connect", sessionId);
            return;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[{SessionId}] Deepgram WebSocket connection timed out after 10 seconds", sessionId);
            await CloseIfOpenAsync(clientWs, logger, sessionId,
                WebSocketCloseStatus.InternalServerError,
                "Transcription service connection timed out.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{SessionId}] Failed to connect to Deepgram", sessionId);
            await CloseIfOpenAsync(clientWs, logger, sessionId,
                WebSocketCloseStatus.InternalServerError,
                "Failed to connect to transcription service.");
            return;
        }

        logger.LogInformation("[{SessionId}] Connected to Deepgram, starting relay tasks", sessionId);

        var sessionConfig = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "SessionConfig",
            multichannel = isMultichannel,
            diarize = deepgramSettings.Diarize,
        });
        var configBytes = Encoding.UTF8.GetBytes(sessionConfig);
        await clientWs.SendAsync(
            new ArraySegment<byte>(configBytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            context.RequestAborted);

        logger.LogInformation(
            "[{SessionId}] Sent SessionConfig to client: multichannel={IsMultichannel}, diarize={Diarize}",
            sessionId, isMultichannel, deepgramSettings.Diarize);

        var lastAudioSentTicks = new StrongBox<long>(DateTimeOffset.UtcNow.Ticks);
        using var deepgramSendLock = new SemaphoreSlim(1, 1);
        using var sessionCts = new CancellationTokenSource();
        using var audioCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts.Token, context.RequestAborted);
        using var resultsCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts.Token, context.RequestAborted);

        var relayAudio = RelayAudioAsync(clientWs, deepgramWs, audioCts, logger, lastAudioSentTicks, deepgramSendLock, sessionId);
        var relayResults = RelayResultsAsync(deepgramWs, clientWs, resultsCts, logger, sessionId);
        var keepAlive = SendKeepAliveAsync(deepgramWs, sessionCts, lastAudioSentTicks, deepgramSendLock, logger, sessionId, deepgramSettings.KeepAliveIntervalSeconds);

        var completed = await Task.WhenAny(relayAudio, relayResults);
        if (completed == relayAudio)
            await resultsCts.CancelAsync();
        else
            await audioCts.CancelAsync();

        await sessionCts.CancelAsync();
        await Task.WhenAll(relayAudio, relayResults, keepAlive);

        await CloseIfOpenAsync(deepgramWs, logger, sessionId);
        await CloseIfOpenAsync(clientWs, logger, sessionId);

        logger.LogInformation("[{SessionId}] Transcription session ended for user {UserId}", sessionId, userId);
    }

    private static async Task RelayAudioAsync(
        WebSocket clientWs,
        ClientWebSocket deepgramWs,
        CancellationTokenSource ownCts,
        ILogger logger,
        StrongBox<long> lastAudioSentTicks,
        SemaphoreSlim deepgramSendLock,
        string sessionId)
    {
        var buffer = new byte[BufferSize];
        long totalBytes = 0;
        long frameCount = 0;

        try
        {
            while (!ownCts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await clientWs.ReceiveAsync(buffer, ownCts.Token);
                }
                catch (WebSocketException ex)
                {
                    logger.LogWarning("[{SessionId}] Client WebSocket receive error: {Message}", sessionId, ex.Message);
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning("[{SessionId}] Client WebSocket in invalid state: {Message}", sessionId, ex.Message);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("[{SessionId}] Client sent close frame, sending CloseStream to Deepgram", sessionId);
                    try
                    {
                        var closeMessage = Encoding.UTF8.GetBytes("{\"type\":\"CloseStream\"}");
                        await deepgramSendLock.WaitAsync(ownCts.Token);
                        try
                        {
                            await deepgramWs.SendAsync(
                                closeMessage,
                                WebSocketMessageType.Text,
                                endOfMessage: true,
                                CancellationToken.None);
                        }
                        finally
                        {
                            deepgramSendLock.Release();
                        }
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Failed to send CloseStream to Deepgram: {Message}", sessionId, ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket not open for CloseStream: {Message}", sessionId, ex.Message);
                    }
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                {
                    var copy = new byte[result.Count];
                    Buffer.BlockCopy(buffer, 0, copy, 0, result.Count);

                    try
                    {
                        await deepgramSendLock.WaitAsync(ownCts.Token);
                        try
                        {
                            await deepgramWs.SendAsync(
                                new ArraySegment<byte>(copy, 0, result.Count),
                                WebSocketMessageType.Binary,
                                result.EndOfMessage,
                                ownCts.Token);
                        }
                        finally
                        {
                            deepgramSendLock.Release();
                        }

                        totalBytes += result.Count;
                        frameCount++;
                        Interlocked.Exchange(ref lastAudioSentTicks.Value, DateTimeOffset.UtcNow.Ticks);
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Failed to send audio to Deepgram: {Message}", sessionId, ex.Message);
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket not open for audio send: {Message}", sessionId, ex.Message);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        logger.LogInformation(
            "[{SessionId}] Audio relay stopped. Total bytes: {TotalBytes}, frames: {FrameCount}",
            sessionId, totalBytes, frameCount);
    }

    private static async Task RelayResultsAsync(
        ClientWebSocket deepgramWs,
        WebSocket clientWs,
        CancellationTokenSource ownCts,
        ILogger logger,
        string sessionId)
    {
        var buffer = new byte[BufferSize];
        long messageCount = 0;

        try
        {
            while (!ownCts.Token.IsCancellationRequested)
            {
                using var messageStream = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    try
                    {
                        result = await deepgramWs.ReceiveAsync(buffer, ownCts.Token);
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket receive error: {Message}", sessionId, ex.Message);
                        return;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket in invalid state: {Message}", sessionId, ex.Message);
                        return;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        logger.LogWarning(
                            "[{SessionId}] Deepgram closed connection: {CloseStatus} - {CloseDescription}",
                            sessionId, deepgramWs.CloseStatus, deepgramWs.CloseStatusDescription);

                        try
                        {
                            var reason = deepgramWs.CloseStatusDescription ?? "Transcription service closed the connection";
                            while (Encoding.UTF8.GetByteCount(reason) > 123)
                            {
                                reason = reason[..^4] + "...";
                            }

                            using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await clientWs.CloseOutputAsync(
                                WebSocketCloseStatus.InternalServerError,
                                reason,
                                closeCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            logger.LogWarning("[{SessionId}] Client close handshake timed out, aborting", sessionId);
                            clientWs.Abort();
                        }
                        catch (WebSocketException ex)
                        {
                            logger.LogWarning("[{SessionId}] Failed to forward close to client: {Message}", sessionId, ex.Message);
                        }
                        catch (InvalidOperationException ex)
                        {
                            logger.LogWarning("[{SessionId}] Client WebSocket not open for close forward: {Message}", sessionId, ex.Message);
                        }

                        return;
                    }

                    if (result.Count > 0)
                    {
                        messageStream.Write(buffer, 0, result.Count);
                    }

                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text && messageStream.Length > 0)
                {
                    var messageBytes = messageStream.ToArray();

                    try
                    {
                        await clientWs.SendAsync(
                            new ArraySegment<byte>(messageBytes),
                            WebSocketMessageType.Text,
                            endOfMessage: true,
                            ownCts.Token);

                        messageCount++;
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Failed to send result to client: {Message}", sessionId, ex.Message);
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Client WebSocket not open for result send: {Message}", sessionId, ex.Message);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        logger.LogInformation(
            "[{SessionId}] Results relay stopped. Total messages forwarded: {MessageCount}",
            sessionId, messageCount);
    }

    private static async Task SendKeepAliveAsync(
        ClientWebSocket deepgramWs,
        CancellationTokenSource sessionCts,
        StrongBox<long> lastAudioSentTicks,
        SemaphoreSlim deepgramSendLock,
        ILogger logger,
        string sessionId,
        int intervalSeconds)
    {
        var keepAliveMessage = Encoding.UTF8.GetBytes("{\"type\":\"KeepAlive\"}");
        var interval = TimeSpan.FromSeconds(Math.Max(intervalSeconds, 1));

        try
        {
            while (!sessionCts.Token.IsCancellationRequested)
            {
                await Task.Delay(interval, sessionCts.Token);

                var lastSentTicks = Interlocked.Read(ref lastAudioSentTicks.Value);
                var elapsed = DateTimeOffset.UtcNow - new DateTimeOffset(lastSentTicks, TimeSpan.Zero);
                if (elapsed > interval)
                {
                    try
                    {
                        await deepgramSendLock.WaitAsync(sessionCts.Token);
                        try
                        {
                            await deepgramWs.SendAsync(
                                keepAliveMessage,
                                WebSocketMessageType.Text,
                                endOfMessage: true,
                                sessionCts.Token);
                        }
                        finally
                        {
                            deepgramSendLock.Release();
                        }

                        logger.LogDebug("[{SessionId}] Sent KeepAlive to Deepgram", sessionId);
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Failed to send KeepAlive to Deepgram: {Message}", sessionId, ex.Message);
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket not open for KeepAlive: {Message}", sessionId, ex.Message);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private static bool IsContainerFormat(string? mimeType, bool treatEmptyAsContainer = true)
    {
        if (string.IsNullOrEmpty(mimeType))
            return treatEmptyAsContainer;

        return mimeType.Contains("webm", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("opus", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("ogg", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("mp4", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLoopbackAddress(string baseUrl)
    {
        return baseUrl.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.StartsWith("127.", StringComparison.Ordinal)
            || baseUrl.StartsWith("[::1]", StringComparison.Ordinal)
            || baseUrl.Equals("::1", StringComparison.Ordinal);
    }

    private static async Task CloseIfOpenAsync(
        WebSocket ws,
        ILogger logger,
        string sessionId,
        WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure,
        string reason = "Done")
    {
        try
        {
            if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await ws.CloseOutputAsync(status, reason, timeoutCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[{SessionId}] WebSocket close timed out, aborting", sessionId);
            ws.Abort();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[{SessionId}] Error closing WebSocket, aborting", sessionId);
            try { ws.Abort(); } catch { /* Best effort */ }
        }
    }
}
