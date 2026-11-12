using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Moq;
using System.Net;

namespace HizzaCoinBackendTests;

public class ProgramStartupTests
{
    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task Program_starts_with_environment_specific_openapi_routes(
        string environment,
        HttpStatusCode expectedStatus)
    {
        using var factory = new StartupFactory(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.NotNull(factory.Services.GetRequiredService<IMongoDatabase>());
        factory.Challenges.Verify(x => x.CancelAllChallenges(), Times.Once);
        factory.Challenges.Verify(x => x.GetAsync(), Times.Once);
    }

    private sealed class StartupFactory(string environment) : WebApplicationFactory<Program>
    {
        public Mock<IChallengesService> Challenges { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureTestServices(services =>
            {
                Challenges.Setup(x => x.CancelAllChallenges()).Returns(Task.CompletedTask);
                Challenges.Setup(x => x.GetAsync()).ReturnsAsync([]);
                services.RemoveAll<IChallengesService>();
                services.AddSingleton(Challenges.Object);
            });
        }
    }
}
