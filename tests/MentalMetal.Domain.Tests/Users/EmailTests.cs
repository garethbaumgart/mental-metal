using MentalMetal.Domain.Users;

namespace MentalMetal.Domain.Tests.Users;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("first.last@domain.co.uk")]
    [InlineData("user+tag@example.com")]
    public void Create_ValidEmail_Succeeds(string value)
    {
        var email = Email.Create(value);

        Assert.Equal(value.Trim().ToLower(), email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("notanemail")]
    [InlineData("@missing-local.com")]
    [InlineData("missing-domain@")]
    [InlineData("missing@tld")]
    public void Create_InvalidEmail_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => Email.Create(value));
    }

    [Fact]
    public void Create_Null_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => Email.Create(null!));
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("TEST@EXAMPLE.COM");

        Assert.Equal(email1, email2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var email1 = Email.Create("a@example.com");
        var email2 = Email.Create("b@example.com");

        Assert.NotEqual(email1, email2);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var email = Email.Create("test@example.com");

        Assert.Equal("test@example.com", email.ToString());
    }
}
