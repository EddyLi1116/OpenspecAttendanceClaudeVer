using AttendanceSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AttendanceSystem.Infrastructure.Security;

public class BcryptOrPbkdf2PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(new User(), password);

    public PasswordVerificationOutcome Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(new User(), hash, password);
        return result switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed
        };
    }
}
