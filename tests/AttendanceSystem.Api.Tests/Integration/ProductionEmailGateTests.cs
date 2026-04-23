using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AttendanceSystem.Api.Tests.Integration;

public class ProductionEmailGateTests
{
    private class ProdFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Mode"] = "FileLog",
                    ["ConnectionStrings:MySql"] = "Server=x;Database=x;User=x;Password=x",
                    ["Jwt:SigningKey"] = "test-signing-key-with-at-least-thirty-two-bytes!!"
                });
            });
        }
    }

    [Fact]
    public void Startup_Throws_WhenProductionAndFileLog()
    {
        using var factory = new ProdFactory();
        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("FileLogEmailSender", ex.Message);
    }
}
