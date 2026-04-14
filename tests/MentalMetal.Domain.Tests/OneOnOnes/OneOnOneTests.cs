using MentalMetal.Domain.OneOnOnes;

namespace MentalMetal.Domain.Tests.OneOnOnes;

public class OneOnOneTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid PersonId = Guid.NewGuid();
    private static readonly DateOnly OccurredAt = new(2026, 4, 10);

    [Fact]
    public void Create_Minimal_RaisesCreatedEvent()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt);

        Assert.Equal(UserId, o.UserId);
        Assert.Equal(PersonId, o.PersonId);
        Assert.Equal(OccurredAt, o.OccurredAt);
        Assert.Null(o.Notes);
        Assert.Null(o.MoodRating);
        Assert.Empty(o.Topics);
        Assert.Empty(o.ActionItems);
        Assert.Empty(o.FollowUps);

        var evt = Assert.IsType<OneOnOneCreated>(Assert.Single(o.DomainEvents));
        Assert.Equal(o.Id, evt.OneOnOneId);
    }

    [Fact]
    public void Create_WithAllFields_StoresAllFields()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt,
            notes: "Great chat",
            topics: new[] { "career", "project-x" },
            moodRating: 4);

        Assert.Equal("Great chat", o.Notes);
        Assert.Equal(4, o.MoodRating);
        Assert.Equal(new[] { "career", "project-x" }, o.Topics);
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OneOnOne.Create(Guid.Empty, PersonId, OccurredAt));
    }

    [Fact]
    public void Create_EmptyPersonId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OneOnOne.Create(UserId, Guid.Empty, OccurredAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_InvalidMoodRating_Throws(int rating)
    {
        Assert.Throws<ArgumentException>(() =>
            OneOnOne.Create(UserId, PersonId, OccurredAt, moodRating: rating));
    }

    [Fact]
    public void Update_NewValues_ReplacesFields()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt,
            topics: new[] { "old" }, moodRating: 3);
        o.ClearDomainEvents();

        o.Update("new notes", new[] { "career" }, 5);

        Assert.Equal("new notes", o.Notes);
        Assert.Equal(new[] { "career" }, o.Topics);
        Assert.Equal(5, o.MoodRating);
        Assert.IsType<OneOnOneUpdated>(Assert.Single(o.DomainEvents));
    }

    [Fact]
    public void Update_ClearingOptionalFields_Works()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt,
            topics: new[] { "old" }, moodRating: 3);

        o.Update(null, Array.Empty<string>(), null);

        Assert.Null(o.Notes);
        Assert.Empty(o.Topics);
        Assert.Null(o.MoodRating);
    }

    [Fact]
    public void AddActionItem_AddsAndRaisesEvent()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt);
        o.ClearDomainEvents();

        var item = o.AddActionItem("Send promotion packet");

        Assert.False(item.Completed);
        Assert.Equal("Send promotion packet", item.Description);
        Assert.Single(o.ActionItems);
        Assert.IsType<ActionItemAdded>(Assert.Single(o.DomainEvents));
    }

    [Fact]
    public void CompleteActionItem_MarksCompleted()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt);
        var item = o.AddActionItem("Item");
        o.ClearDomainEvents();

        o.CompleteActionItem(item.Id);

        Assert.True(o.ActionItems[0].Completed);
        Assert.IsType<ActionItemCompleted>(Assert.Single(o.DomainEvents));
    }

    [Fact]
    public void RemoveActionItem_Removes()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt);
        var item = o.AddActionItem("Item");
        o.ClearDomainEvents();

        o.RemoveActionItem(item.Id);

        Assert.Empty(o.ActionItems);
        Assert.IsType<ActionItemRemoved>(Assert.Single(o.DomainEvents));
    }

    [Fact]
    public void RemoveActionItem_NotFound_Throws()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt);
        Assert.Throws<ArgumentException>(() => o.RemoveActionItem(Guid.NewGuid()));
    }

    [Fact]
    public void AddFollowUp_AddsAndRaisesEvent()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt);
        o.ClearDomainEvents();

        var fu = o.AddFollowUp("Check training budget");

        Assert.False(fu.Resolved);
        Assert.IsType<FollowUpAdded>(Assert.Single(o.DomainEvents));
    }

    [Fact]
    public void ResolveFollowUp_MarksResolved()
    {
        var o = OneOnOne.Create(UserId, PersonId, OccurredAt);
        var fu = o.AddFollowUp("Follow up");
        o.ClearDomainEvents();

        o.ResolveFollowUp(fu.Id);

        Assert.True(o.FollowUps[0].Resolved);
        Assert.IsType<FollowUpResolved>(Assert.Single(o.DomainEvents));
    }
}
