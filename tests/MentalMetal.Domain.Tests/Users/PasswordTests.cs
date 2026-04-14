using MentalMetal.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MentalMetal.Domain.Tests.Users;

public class PasswordTests
{
    private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

    [Fact]
    public void Create_ValidPlaintext_ProducesHash()
    {
        var password = Password.Create("correct-horse-battery", Hasher);

        Assert.False(string.IsNullOrWhiteSpace(password.HashValue));
        Assert.NotEqual("correct-horse-battery", password.HashValue);
    }

    [Fact]
    public void Create_And_Verify_RoundTrip()
    {
        var password = Password.Create("secret-pw", Hasher);

        Assert.True(password.Verify("secret-pw", Hasher));
        Assert.False(password.Verify("wrong-pw", Hasher));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Create_ShortOrEmpty_Throws(string plaintext)
    {
        Assert.ThrowsAny<ArgumentException>(() => Password.Create(plaintext, Hasher));
    }

    [Fact]
    public void Create_NullHasher_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Password.Create("valid-pw-12", null!));
    }

    [Fact]
    public void CreateFromHash_PreservesHash()
    {
        var original = Password.Create("another-pw", Hasher);
        var rehydrated = Password.CreateFromHash(original.HashValue);

        Assert.Equal(original.HashValue, rehydrated.HashValue);
        Assert.True(rehydrated.Verify("another-pw", Hasher));
    }

    [Fact]
    public void Verify_EmptyPlaintext_ReturnsFalse()
    {
        var password = Password.Create("secret-pw", Hasher);

        Assert.False(password.Verify("", Hasher));
        Assert.False(password.Verify(null!, Hasher));
    }

    [Fact]
    public void Equality_SameHash_AreEqual()
    {
        var hash = Password.Create("pw-12345", Hasher).HashValue;
        var a = Password.CreateFromHash(hash);
        var b = Password.CreateFromHash(hash);

        Assert.Equal(a, b);
    }
}
