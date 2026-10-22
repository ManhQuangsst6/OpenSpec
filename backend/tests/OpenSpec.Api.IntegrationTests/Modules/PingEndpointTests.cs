using System.Net;
using OpenSpec.Api.IntegrationTests.Fixtures;

namespace OpenSpec.Api.IntegrationTests.Modules;

public class PingEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("users")]
    [InlineData("catalog")]
    [InlineData("orders")]
    public async Task Ping_ReturnsOk(string module)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/{module}/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
