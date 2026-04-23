namespace AttendanceSystem.Infrastructure.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    PasswordVerificationOutcome Verify(string hash, string password);
}

public enum PasswordVerificationOutcome
{
    Failed,
    Success,
    SuccessRehashNeeded
}
