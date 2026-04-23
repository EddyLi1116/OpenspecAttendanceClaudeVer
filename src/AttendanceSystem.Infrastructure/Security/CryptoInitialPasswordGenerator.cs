using System.Security.Cryptography;
using AttendanceSystem.Domain.Security;

namespace AttendanceSystem.Infrastructure.Security;

public class CryptoInitialPasswordGenerator : IInitialPasswordGenerator
{
    private const int Length = 16;
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%^&*?-_";
    private const string All = Upper + Lower + Digits + Symbols;

    public string Generate()
    {
        var chars = new char[Length];
        chars[0] = PickOne(Upper);
        chars[1] = PickOne(Lower);
        chars[2] = PickOne(Digits);
        chars[3] = PickOne(Symbols);
        for (int i = 4; i < Length; i++)
            chars[i] = PickOne(All);

        // Shuffle to avoid positional bias (Fisher–Yates with cryptographic RNG).
        for (int i = Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static char PickOne(string pool) => pool[RandomNumberGenerator.GetInt32(pool.Length)];
}
