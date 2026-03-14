using Xunit;
using WLGPO.GPO;

namespace WLGPO.Tests;

public class LocalPrincipalSidTests
{
    // ── Named SIDs ─────────────────────────────────────────────────

    [Theory]
    [InlineData("Administrators",        "S-1-5-32-544")]
    [InlineData("Users",                 "S-1-5-32-545")]
    [InlineData("Guests",                "S-1-5-32-546")]
    [InlineData("PowerUsers",            "S-1-5-32-547")]
    [InlineData("BackupOperators",       "S-1-5-32-551")]
    [InlineData("RemoteDesktopUsers",    "S-1-5-32-555")]
    [InlineData("NetworkConfigOps",      "S-1-5-32-556")]
    [InlineData("HyperVAdministrators",  "S-1-5-32-578")]
    [InlineData("RemoteManagementUsers", "S-1-5-32-580")]
    [InlineData("System",                "S-1-5-18")]
    [InlineData("LocalService",          "S-1-5-19")]
    [InlineData("NetworkService",        "S-1-5-20")]
    [InlineData("Iusr",                  "S-1-5-17")]
    [InlineData("Everyone",              "S-1-1-0")]
    public void TryParse_WellKnownName_ReturnsCorrectSid(string name, string expectedSid)
    {
        var result = LocalPrincipalSid.TryParse(name, out var sid);
        Assert.True(result);
        Assert.Equal(expectedSid, sid.Value);
    }

    // ── Case insensitivity ─────────────────────────────────────────

    [Theory]
    [InlineData("administrators")]
    [InlineData("ADMINISTRATORS")]
    [InlineData("Administrators")]
    [InlineData("aDmInIsTrAtOrS")]
    public void TryParse_NameCaseInsensitive(string input)
    {
        var result = LocalPrincipalSid.TryParse(input, out var sid);
        Assert.True(result);
        Assert.Equal("S-1-5-32-544", sid.Value);
    }

    // ── Whitespace trimming ────────────────────────────────────────

    [Fact]
    public void TryParse_NameWithLeadingTrailingWhitespace_Succeeds()
    {
        var result = LocalPrincipalSid.TryParse("  Administrators  ", out var sid);
        Assert.True(result);
        Assert.Equal("S-1-5-32-544", sid.Value);
    }

    // ── Raw SID strings ────────────────────────────────────────────

    [Theory]
    [InlineData("S-1-5-32-544")]
    [InlineData("S-1-5-18")]
    [InlineData("S-1-1-0")]
    public void TryParse_RawSidString_Succeeds(string rawSid)
    {
        var result = LocalPrincipalSid.TryParse(rawSid, out var sid);
        Assert.True(result);
        Assert.Equal(rawSid, sid.Value);
    }

    [Fact]
    public void TryParse_RawSidString_WithWhitespace_Succeeds()
    {
        var result = LocalPrincipalSid.TryParse("  S-1-5-32-544  ", out var sid);
        Assert.True(result);
        Assert.Equal("S-1-5-32-544", sid.Value);
    }

    // ── Invalid inputs ─────────────────────────────────────────────

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        var result = LocalPrincipalSid.TryParse(string.Empty, out _);
        Assert.False(result);
    }

    [Fact]
    public void TryParse_Whitespace_ReturnsFalse()
    {
        var result = LocalPrincipalSid.TryParse("   ", out _);
        Assert.False(result);
    }

    [Theory]
    [InlineData("NotAGroup")]
    [InlineData("S-1-INVALID")]
    [InlineData("1234")]
    [InlineData("HKLM\\Software")]
    public void TryParse_InvalidInput_ReturnsFalse(string input)
    {
        var result = LocalPrincipalSid.TryParse(input, out _);
        Assert.False(result);
    }

    // ── Default value ──────────────────────────────────────────────

    [Fact]
    public void Default_ValueIsNull()
    {
        var sid = default(LocalPrincipalSid);
        Assert.Null(sid.Value);
    }

    // ── ToString ───────────────────────────────────────────────────

    [Fact]
    public void ToString_ReturnsValue()
    {
        LocalPrincipalSid.TryParse("Administrators", out var sid);
        Assert.Equal("S-1-5-32-544", sid.ToString());
    }
}
