using MentalMetal.Domain.Observations;

namespace MentalMetal.Domain.Tests.Observations;

public class ObservationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PersonId = Guid.NewGuid();

    [Fact]
    public void Create_Valid_RaisesCreatedEvent()
    {
        var obs = Observation.Create(UserId, PersonId, "Led incident response", ObservationTag.Win);

        Assert.Equal(UserId, obs.UserId);
        Assert.Equal(PersonId, obs.PersonId);
        Assert.Equal("Led incident response", obs.Description);
        Assert.Equal(ObservationTag.Win, obs.Tag);
        Assert.NotEqual(default, obs.OccurredAt);

        var evt = Assert.IsType<ObservationCreated>(Assert.Single(obs.DomainEvents));
        Assert.Equal(obs.Id, evt.ObservationId);
    }

    [Fact]
    public void Create_WithOccurredAt_UsesProvidedDate()
    {
        var date = new DateOnly(2026, 4, 1);
        var obs = Observation.Create(UserId, PersonId, "desc", ObservationTag.Concern, date);

        Assert.Equal(date, obs.OccurredAt);
    }

    [Fact]
    public void Create_WithSourceCaptureId_StoresLink()
    {
        var captureId = Guid.NewGuid();
        var obs = Observation.Create(UserId, PersonId, "desc", ObservationTag.Growth, null, captureId);

        Assert.Equal(captureId, obs.SourceCaptureId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyDescription_Throws(string? description)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Observation.Create(UserId, PersonId, description!, ObservationTag.Win));
    }

    [Fact]
    public void Create_EmptyPersonId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Observation.Create(UserId, Guid.Empty, "desc", ObservationTag.Win));
    }

    [Fact]
    public void Update_ValidInput_UpdatesFields()
    {
        var obs = Observation.Create(UserId, PersonId, "old", ObservationTag.Growth);
        obs.ClearDomainEvents();

        obs.Update("new", ObservationTag.Win);

        Assert.Equal("new", obs.Description);
        Assert.Equal(ObservationTag.Win, obs.Tag);
        Assert.IsType<ObservationUpdated>(Assert.Single(obs.DomainEvents));
    }

    [Fact]
    public void Update_EmptyDescription_Throws()
    {
        var obs = Observation.Create(UserId, PersonId, "old", ObservationTag.Growth);

        Assert.ThrowsAny<ArgumentException>(() => obs.Update("", ObservationTag.Win));
    }

    [Fact]
    public void MarkDeleted_RaisesDeletedEvent()
    {
        var obs = Observation.Create(UserId, PersonId, "desc", ObservationTag.Win);
        obs.ClearDomainEvents();

        obs.MarkDeleted();

        Assert.IsType<ObservationDeleted>(Assert.Single(obs.DomainEvents));
    }
}
