using Korendzh.Infrastructure.Auth;

namespace Korendzh.Tests;

public class TokenHasherTests
{
    [Fact]
    public void NewToken_returns_distinct_raw_and_matching_hash()
    {
        var (raw1, hash1) = TokenHasher.NewToken();
        var (raw2, hash2) = TokenHasher.NewToken();

        Assert.NotEqual(raw1, raw2);
        Assert.NotEqual(hash1, hash2);
        Assert.Equal(hash1, TokenHasher.Hash(raw1));
        Assert.Equal(hash2, TokenHasher.Hash(raw2));
    }

    [Fact]
    public void Hash_is_deterministic_and_lowercase_hex()
    {
        var raw = "fixed-input-token";
        var h1 = TokenHasher.Hash(raw);
        var h2 = TokenHasher.Hash(raw);

        Assert.Equal(h1, h2);
        Assert.Equal(h1, h1.ToLowerInvariant());
        Assert.Matches("^[0-9a-f]{64}$", h1);
    }

    [Fact]
    public void NewToken_default_length_is_url_safe_base64()
    {
        var (raw, _) = TokenHasher.NewToken();
        Assert.DoesNotContain('+', raw);
        Assert.DoesNotContain('/', raw);
        Assert.DoesNotContain('=', raw);
        Assert.True(raw.Length >= 32);
    }
}
