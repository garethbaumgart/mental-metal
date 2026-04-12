using MentalMetal.Domain.People;

namespace MentalMetal.Domain.Tests.People;

public class PersonTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Theory]
    [InlineData(PersonType.DirectReport)]
    [InlineData(PersonType.Stakeholder)]
    [InlineData(PersonType.Candidate)]
    public void Create_ValidInputs_CreatesPersonWithCorrectState(PersonType type)
    {
        var person = Person.Create(UserId, "Alice Smith", type, "alice@example.com", "Engineer");

        Assert.NotEqual(Guid.Empty, person.Id);
        Assert.Equal(UserId, person.UserId);
        Assert.Equal("Alice Smith", person.Name);
        Assert.Equal(type, person.Type);
        Assert.Equal("alice@example.com", person.Email);
        Assert.Equal("Engineer", person.Role);
        Assert.Null(person.Team);
        Assert.Null(person.Notes);
        Assert.False(person.IsArchived);
        Assert.Null(person.ArchivedAt);
    }

    [Fact]
    public void Create_DirectReport_HasNoCareerDetailsOrCandidateDetails()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);

        Assert.Null(person.CareerDetails);
        Assert.Null(person.CandidateDetails);
    }

    [Fact]
    public void Create_Stakeholder_HasNoTypeSpecificDetails()
    {
        var person = Person.Create(UserId, "Bob", PersonType.Stakeholder);

        Assert.Null(person.CareerDetails);
        Assert.Null(person.CandidateDetails);
    }

    [Fact]
    public void Create_Candidate_InitialisesCandidateDetailsWithNewStatus()
    {
        var person = Person.Create(UserId, "Carol", PersonType.Candidate);

        Assert.NotNull(person.CandidateDetails);
        Assert.Equal(PipelineStatus.New, person.CandidateDetails.PipelineStatus);
        Assert.Null(person.CareerDetails);
    }

    [Fact]
    public void Create_RaisesPersonCreatedEvent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);

        var domainEvent = Assert.Single(person.DomainEvents);
        var created = Assert.IsType<PersonCreated>(domainEvent);
        Assert.Equal(person.Id, created.PersonId);
        Assert.Equal(UserId, created.UserId);
        Assert.Equal("Alice", created.Name);
        Assert.Equal(PersonType.DirectReport, created.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyName_Throws(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Person.Create(UserId, name!, PersonType.DirectReport));
    }

    [Fact]
    public void UpdateProfile_SetsFieldsAndRaisesEvent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.ClearDomainEvents();

        person.UpdateProfile("Alice Updated", "alice@new.com", "Senior Engineer", "Platform", "Great performer");

        Assert.Equal("Alice Updated", person.Name);
        Assert.Equal("alice@new.com", person.Email);
        Assert.Equal("Senior Engineer", person.Role);
        Assert.Equal("Platform", person.Team);
        Assert.Equal("Great performer", person.Notes);

        var domainEvent = Assert.Single(person.DomainEvents);
        Assert.IsType<PersonProfileUpdated>(domainEvent);
    }

    [Fact]
    public void ChangeType_DirectReportToCandidate_ClearsCareerDetailsInitialisesCandidate()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.UpdateCareerDetails("Senior", "Management", "Public speaking");
        person.ClearDomainEvents();

        person.ChangeType(PersonType.Candidate);

        Assert.Equal(PersonType.Candidate, person.Type);
        Assert.Null(person.CareerDetails);
        Assert.NotNull(person.CandidateDetails);
        Assert.Equal(PipelineStatus.New, person.CandidateDetails.PipelineStatus);

        var domainEvent = Assert.Single(person.DomainEvents);
        var changed = Assert.IsType<PersonTypeChanged>(domainEvent);
        Assert.Equal(PersonType.DirectReport, changed.OldType);
        Assert.Equal(PersonType.Candidate, changed.NewType);
    }

    [Fact]
    public void ChangeType_CandidateToDirectReport_ClearsCandidateDetails()
    {
        var person = Person.Create(UserId, "Alice", PersonType.Candidate);
        person.ClearDomainEvents();

        person.ChangeType(PersonType.DirectReport);

        Assert.Equal(PersonType.DirectReport, person.Type);
        Assert.Null(person.CandidateDetails);
    }

    [Fact]
    public void ChangeType_SameType_IsNoOp()
    {
        var person = Person.Create(UserId, "Alice", PersonType.Stakeholder);
        person.ClearDomainEvents();

        person.ChangeType(PersonType.Stakeholder);

        Assert.Empty(person.DomainEvents);
    }

    [Fact]
    public void UpdateCareerDetails_OnDirectReport_Succeeds()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.ClearDomainEvents();

        person.UpdateCareerDetails("Senior", "Leadership", "Communication");

        Assert.NotNull(person.CareerDetails);
        Assert.Equal("Senior", person.CareerDetails.Level);
        Assert.Equal("Leadership", person.CareerDetails.Aspirations);
        Assert.Equal("Communication", person.CareerDetails.GrowthAreas);

        var domainEvent = Assert.Single(person.DomainEvents);
        Assert.IsType<CareerDetailsUpdated>(domainEvent);
    }

    [Theory]
    [InlineData(PersonType.Stakeholder)]
    [InlineData(PersonType.Candidate)]
    public void UpdateCareerDetails_OnNonDirectReport_Throws(PersonType type)
    {
        var person = Person.Create(UserId, "Alice", type);

        Assert.ThrowsAny<ArgumentException>(() =>
            person.UpdateCareerDetails("Senior", null, null));
    }

    [Fact]
    public void UpdateCandidateDetails_OnCandidate_Succeeds()
    {
        var person = Person.Create(UserId, "Alice", PersonType.Candidate);
        person.ClearDomainEvents();

        person.UpdateCandidateDetails("Strong CV", "LinkedIn");

        Assert.NotNull(person.CandidateDetails);
        Assert.Equal("Strong CV", person.CandidateDetails.CvNotes);
        Assert.Equal("LinkedIn", person.CandidateDetails.SourceChannel);
        Assert.Equal(PipelineStatus.New, person.CandidateDetails.PipelineStatus);

        var domainEvent = Assert.Single(person.DomainEvents);
        Assert.IsType<CandidateDetailsUpdated>(domainEvent);
    }

    [Theory]
    [InlineData(PersonType.DirectReport)]
    [InlineData(PersonType.Stakeholder)]
    public void UpdateCandidateDetails_OnNonCandidate_Throws(PersonType type)
    {
        var person = Person.Create(UserId, "Alice", type);

        Assert.ThrowsAny<ArgumentException>(() =>
            person.UpdateCandidateDetails("Notes", "Referral"));
    }

    [Theory]
    [InlineData(PipelineStatus.New, PipelineStatus.Screening)]
    [InlineData(PipelineStatus.Screening, PipelineStatus.Interviewing)]
    [InlineData(PipelineStatus.Interviewing, PipelineStatus.OfferStage)]
    [InlineData(PipelineStatus.OfferStage, PipelineStatus.Hired)]
    public void AdvanceCandidatePipeline_ValidForwardTransitions_Succeeds(
        PipelineStatus from, PipelineStatus to)
    {
        var person = Person.Create(UserId, "Alice", PersonType.Candidate);

        // Advance to the 'from' state
        AdvanceToStatus(person, from);
        person.ClearDomainEvents();

        person.AdvanceCandidatePipeline(to);

        Assert.Equal(to, person.CandidateDetails!.PipelineStatus);

        var domainEvent = Assert.Single(person.DomainEvents);
        var advanced = Assert.IsType<CandidatePipelineAdvanced>(domainEvent);
        Assert.Equal(from, advanced.OldStatus);
        Assert.Equal(to, advanced.NewStatus);
    }

    [Theory]
    [InlineData(PipelineStatus.New)]
    [InlineData(PipelineStatus.Screening)]
    [InlineData(PipelineStatus.Interviewing)]
    [InlineData(PipelineStatus.OfferStage)]
    public void AdvanceCandidatePipeline_RejectFromAnyActiveState_Succeeds(PipelineStatus from)
    {
        var person = Person.Create(UserId, "Alice", PersonType.Candidate);
        AdvanceToStatus(person, from);

        person.AdvanceCandidatePipeline(PipelineStatus.Rejected);

        Assert.Equal(PipelineStatus.Rejected, person.CandidateDetails!.PipelineStatus);
    }

    [Theory]
    [InlineData(PipelineStatus.New)]
    [InlineData(PipelineStatus.Screening)]
    [InlineData(PipelineStatus.Interviewing)]
    [InlineData(PipelineStatus.OfferStage)]
    public void AdvanceCandidatePipeline_WithdrawFromAnyActiveState_Succeeds(PipelineStatus from)
    {
        var person = Person.Create(UserId, "Alice", PersonType.Candidate);
        AdvanceToStatus(person, from);

        person.AdvanceCandidatePipeline(PipelineStatus.Withdrawn);

        Assert.Equal(PipelineStatus.Withdrawn, person.CandidateDetails!.PipelineStatus);
    }

    [Fact]
    public void AdvanceCandidatePipeline_SkippingStages_Throws()
    {
        var person = Person.Create(UserId, "Alice", PersonType.Candidate);
        person.AdvanceCandidatePipeline(PipelineStatus.Screening);

        Assert.ThrowsAny<ArgumentException>(() =>
            person.AdvanceCandidatePipeline(PipelineStatus.OfferStage));
    }

    [Theory]
    [InlineData(PipelineStatus.Hired)]
    [InlineData(PipelineStatus.Rejected)]
    [InlineData(PipelineStatus.Withdrawn)]
    public void AdvanceCandidatePipeline_FromTerminalState_Throws(PipelineStatus terminalStatus)
    {
        var person = Person.Create(UserId, "Alice", PersonType.Candidate);
        AdvanceToStatus(person, terminalStatus);

        Assert.ThrowsAny<ArgumentException>(() =>
            person.AdvanceCandidatePipeline(PipelineStatus.Screening));
    }

    [Theory]
    [InlineData(PersonType.DirectReport)]
    [InlineData(PersonType.Stakeholder)]
    public void AdvanceCandidatePipeline_OnNonCandidate_Throws(PersonType type)
    {
        var person = Person.Create(UserId, "Alice", type);

        Assert.ThrowsAny<ArgumentException>(() =>
            person.AdvanceCandidatePipeline(PipelineStatus.Screening));
    }

    [Fact]
    public void Archive_SetsIsArchivedAndRaisesEvent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.ClearDomainEvents();

        person.Archive();

        Assert.True(person.IsArchived);
        Assert.NotNull(person.ArchivedAt);

        var domainEvent = Assert.Single(person.DomainEvents);
        Assert.IsType<PersonArchived>(domainEvent);
    }

    [Fact]
    public void Archive_AlreadyArchived_IsIdempotent()
    {
        var person = Person.Create(UserId, "Alice", PersonType.DirectReport);
        person.Archive();
        person.ClearDomainEvents();

        person.Archive();

        Assert.True(person.IsArchived);
        Assert.Empty(person.DomainEvents);
    }

    private static void AdvanceToStatus(Person person, PipelineStatus target)
    {
        var path = new[] { PipelineStatus.Screening, PipelineStatus.Interviewing, PipelineStatus.OfferStage, PipelineStatus.Hired };

        if (target is PipelineStatus.Rejected)
        {
            // Stay at New, then reject
            person.AdvanceCandidatePipeline(PipelineStatus.Rejected);
            return;
        }

        if (target is PipelineStatus.Withdrawn)
        {
            person.AdvanceCandidatePipeline(PipelineStatus.Withdrawn);
            return;
        }

        foreach (var step in path)
        {
            if (person.CandidateDetails!.PipelineStatus == target)
                break;
            person.AdvanceCandidatePipeline(step);
        }
    }
}
