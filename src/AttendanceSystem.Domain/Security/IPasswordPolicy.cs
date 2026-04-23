namespace AttendanceSystem.Domain.Security;

public interface IPasswordPolicy
{
    PasswordPolicyResult Validate(string password);
}

public record PasswordPolicyResult(bool IsValid, IReadOnlyList<string> Violations)
{
    public static PasswordPolicyResult Ok() => new(true, Array.Empty<string>());
    public static PasswordPolicyResult Fail(params string[] violations) => new(false, violations);
}
