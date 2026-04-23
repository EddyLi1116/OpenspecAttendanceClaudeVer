using AttendanceSystem.Domain.Security;

namespace AttendanceSystem.Infrastructure.Security;

public class SystemPasswordPolicy : IPasswordPolicy
{
    private const int MinLength = 10;
    internal const string SymbolChars = "!@#$%^&*?-_+=()[]{}|:;,.<>/~";

    public PasswordPolicyResult Validate(string password)
    {
        var violations = new List<string>();
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            violations.Add($"至少 {MinLength} 字元");
        if (password is null || !password.Any(char.IsUpper))
            violations.Add("需包含大寫英文字母");
        if (password is null || !password.Any(char.IsLower))
            violations.Add("需包含小寫英文字母");
        if (password is null || !password.Any(char.IsDigit))
            violations.Add("需包含數字");
        if (password is null || !password.Any(c => SymbolChars.Contains(c)))
            violations.Add("需包含符號");

        return violations.Count == 0
            ? PasswordPolicyResult.Ok()
            : PasswordPolicyResult.Fail(violations.ToArray());
    }
}
