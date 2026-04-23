using AttendanceSystem.Infrastructure.Security;
using Xunit;

namespace AttendanceSystem.Domain.Tests.Security;

public class SystemPasswordPolicyTests
{
    private readonly SystemPasswordPolicy _policy = new();

    [Fact]
    public void TooShort_IsInvalid()
    {
        var r = _policy.Validate("Aa1!aa");
        Assert.False(r.IsValid);
        Assert.Contains(r.Violations, v => v.Contains("字元"));
    }

    [Fact]
    public void MissingUpper_IsInvalid()
    {
        var r = _policy.Validate("password1!xx");
        Assert.False(r.IsValid);
        Assert.Contains(r.Violations, v => v.Contains("大寫"));
    }

    [Fact]
    public void MissingLower_IsInvalid()
    {
        var r = _policy.Validate("PASSWORD1!XX");
        Assert.False(r.IsValid);
        Assert.Contains(r.Violations, v => v.Contains("小寫"));
    }

    [Fact]
    public void MissingDigit_IsInvalid()
    {
        var r = _policy.Validate("Password!xxxx");
        Assert.False(r.IsValid);
        Assert.Contains(r.Violations, v => v.Contains("數字"));
    }

    [Fact]
    public void MissingSymbol_IsInvalid()
    {
        var r = _policy.Validate("Password1xxxx");
        Assert.False(r.IsValid);
        Assert.Contains(r.Violations, v => v.Contains("符號"));
    }

    [Fact]
    public void AllRulesSatisfied_IsValid()
    {
        var r = _policy.Validate("Password1!xx");
        Assert.True(r.IsValid);
        Assert.Empty(r.Violations);
    }
}
