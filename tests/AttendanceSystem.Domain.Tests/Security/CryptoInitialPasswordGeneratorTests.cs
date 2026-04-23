using AttendanceSystem.Infrastructure.Security;
using Xunit;

namespace AttendanceSystem.Domain.Tests.Security;

public class CryptoInitialPasswordGeneratorTests
{
    private const string Symbols = "!@#$%^&*?-_";

    [Fact]
    public void Generate_ProducesSixteenCharsWithAllCharClasses_OverThousandIterations()
    {
        var gen = new CryptoInitialPasswordGenerator();
        for (int i = 0; i < 1000; i++)
        {
            var pwd = gen.Generate();
            Assert.Equal(16, pwd.Length);
            Assert.Contains(pwd, char.IsUpper);
            Assert.Contains(pwd, char.IsLower);
            Assert.Contains(pwd, char.IsDigit);
            Assert.Contains(pwd, c => Symbols.Contains(c));
        }
    }
}
