using Korendzh.Domain;

namespace Korendzh.Tests;

public class InvitationTokenTests
{
    [Fact]
    public void IsValid_true_when_not_consumed_and_not_expired()
    {
        var t = new InvitationToken { ExpiresAt = DateTime.UtcNow.AddDays(1) };
        Assert.True(t.IsValid(DateTime.UtcNow));
    }

    [Fact]
    public void IsValid_false_when_expired()
    {
        var t = new InvitationToken { ExpiresAt = DateTime.UtcNow.AddMinutes(-1) };
        Assert.False(t.IsValid(DateTime.UtcNow));
    }

    [Fact]
    public void IsValid_false_when_consumed()
    {
        var t = new InvitationToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            ConsumedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        Assert.False(t.IsValid(DateTime.UtcNow));
    }
}

public class PasswordResetTokenTests
{
    [Fact]
    public void IsValid_true_for_fresh_token()
    {
        var t = new PasswordResetToken { ExpiresAt = DateTime.UtcNow.AddMinutes(30) };
        Assert.True(t.IsValid(DateTime.UtcNow));
    }

    [Fact]
    public void IsValid_false_after_expiry()
    {
        var t = new PasswordResetToken { ExpiresAt = DateTime.UtcNow.AddMinutes(-1) };
        Assert.False(t.IsValid(DateTime.UtcNow));
    }
}
