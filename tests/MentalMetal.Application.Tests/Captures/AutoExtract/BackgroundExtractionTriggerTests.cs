using MentalMetal.Application.Captures.AutoExtract;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace MentalMetal.Application.Tests.Captures.AutoExtract;

public class BackgroundExtractionTriggerTests
{
    [Fact]
    public async Task FireAndForget_SetsUserIdOnBackgroundScope()
    {
        var userId = Guid.NewGuid();
        var captureId = Guid.NewGuid();

        // Track whether SetUserId was called with the right value
        Guid? capturedUserId = null;

        var services = new ServiceCollection();

        // Register a real CurrentUserService-like stub that records the call
        var backgroundUserScope = Substitute.For<IBackgroundUserScope>();
        backgroundUserScope.When(x => x.SetUserId(Arg.Any<Guid>()))
            .Do(ci => capturedUserId = ci.Arg<Guid>());
        services.AddScoped(_ => backgroundUserScope);

        // Register a minimal AutoExtractCaptureHandler with stubs
        var captureRepo = Substitute.For<ICaptureRepository>();
        // Return null so the handler throws early — that's fine, we're testing scope setup
        captureRepo.GetByIdAsync(captureId, Arg.Any<CancellationToken>())
            .Returns((Capture?)null);

        services.AddScoped(_ => captureRepo);
        services.AddScoped(_ => Substitute.For<IPersonRepository>());
        services.AddScoped(_ => Substitute.For<IInitiativeRepository>());
        services.AddScoped(_ => Substitute.For<ICommitmentRepository>());
        services.AddScoped(_ => Substitute.For<IUserRepository>());
        services.AddScoped(_ => Substitute.For<IAiCompletionService>());
        services.AddScoped(_ => Substitute.For<ITasteBudgetService>());
        services.AddScoped<ICurrentUserService>(_ => backgroundUserScope as ICurrentUserService
            ?? Substitute.For<ICurrentUserService>());
        services.AddScoped<NameResolutionService>();
        services.AddScoped<InitiativeTaggingService>();
        services.AddScoped(_ => Substitute.For<IUnitOfWork>());
        services.AddScoped(_ => NullLogger<AutoExtractCaptureHandler>.Instance);
        services.AddScoped<AutoExtractCaptureHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var trigger = new BackgroundExtractionTrigger(
            scopeFactory, NullLogger<BackgroundExtractionTrigger>.Instance);

        // Act
        trigger.FireAndForget(captureId, userId);
        await Task.Delay(300); // Let background task run

        // Assert — SetUserId was called with the correct user
        Assert.Equal(userId, capturedUserId);
    }
}
