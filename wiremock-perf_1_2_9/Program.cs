using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace wiremock_perf
{
class Program
{
    static async Task Main(string[] args)
    {
        const string AccessToken = "api.token";
        var wiremock = WireMockServer.Start();

        wiremock.Given(Request.Create().WithPath("/connect/token").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new { access_token = AccessToken }));

        wiremock.Given(Request.Create().WithPath("/api/v1/users").WithHeader("Authorization", $"Bearer {AccessToken}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new { id = Guid.NewGuid(), reference = Guid.NewGuid().ToString() }));

        var client1 = CreateHttpClient(wiremock);
        var client2 = CreateHttpClient(wiremock);
        for (int i = 0; i < 5; i++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var response = await client1.GetAsync("/connect/token");
            Console.WriteLine($"/connect/token {stopwatch.Elapsed}");
            var token = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync());

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);
            stopwatch = Stopwatch.StartNew();
            await client2.SendAsync(request);
            Console.WriteLine($"/api/v1/user {stopwatch.Elapsed}");
            Console.WriteLine("=========================");
        }
    }

    private static HttpClient CreateHttpClient(WireMockServer wiremock)
    {
        return new HttpClient()
        {
            BaseAddress = new Uri(wiremock.Urls[0])
        };
    }

    private class TokenResponse
    {
        public string access_token { get; set; }
    }
}
}
