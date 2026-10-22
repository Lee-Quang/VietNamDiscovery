using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace VietnamDiscovery.Api.Tests;

public sealed class ApiErrorTests
{
    private static HttpClient CreateClient(ApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Health_endpoint_preserves_success_response()
    {
        await using var factory = new ApiFactory();
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Weather_endpoint_preserves_success_response()
    {
        await using var factory = new ApiFactory();
        using var client = CreateClient(factory);
        var body = await client.GetFromJsonAsync<JsonElement>("/WeatherForecast");
        Assert.Equal(5, body.GetArrayLength());
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task Swagger_is_available_in_development(string path)
    {
        await using var factory = new ApiFactory();
        using var client = CreateClient(factory);
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        if (path.EndsWith(".json"))
        {
            using var document = JsonDocument.Parse(body);
            Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/v1/health", out _));
            Assert.DoesNotContain("_tests", body);
        }
        else
        {
            Assert.Contains("Swagger UI", body);
        }
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/html")]
    public async Task Missing_route_returns_problem_details_even_for_browser_accept_header(string accept)
    {
        await using var factory = new ApiFactory();
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);
        using var response = await client.GetAsync("/api/v1/missing");
        await AssertProblem(response, HttpStatusCode.NotFound, "/api/v1/missing");
    }

    [Fact]
    public async Task Wrong_method_returns_405_and_retains_allow_header()
    {
        await using var factory = new ApiFactory();
        using var client = CreateClient(factory);
        using var response = await client.PostAsync("/api/v1/health", null);
        await AssertProblem(response, HttpStatusCode.MethodNotAllowed, "/api/v1/health");
        Assert.Contains("GET", response.Content.Headers.Allow);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{invalid-json")]
    public async Task Invalid_input_returns_validation_problem_details(string json)
    {
        await using var factory = new ApiFactory();
        using var client = CreateClient(factory);
        using var response = await client.PostAsync("/_tests/validation",
            new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await AssertProblem(response, HttpStatusCode.BadRequest, "/_tests/validation");
        Assert.NotEmpty(body.GetProperty("errors").EnumerateObject());
    }

    [Theory]
    [InlineData("Development", "application/json")]
    [InlineData("Development", "text/html")]
    [InlineData("Production", "application/json")]
    public async Task Unhandled_exception_returns_safe_500(string environment, string accept)
    {
        await using var factory = new ApiFactory(environment, throwOnHealth: true);
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);
        using var response = await client.GetAsync("/api/v1/health");
        await AssertProblem(response, HttpStatusCode.InternalServerError, "/api/v1/health");
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SECRET_TEST_EXCEPTION", json);
        Assert.DoesNotContain("InvalidOperationException", json);
        Assert.DoesNotContain("stackTrace", json);
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task Swagger_is_not_exposed_in_production(string path)
    {
        await using var factory = new ApiFactory("Production");
        using var client = CreateClient(factory);
        using var response = await client.GetAsync(path);
        await AssertProblem(response, HttpStatusCode.NotFound, path);
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Production", false)]
    [InlineData("Production", true)]
    public async Task Diagnostic_route_requires_development_and_explicit_opt_in(
        string environment, bool enabled)
    {
        await using var factory = new ApiFactory(environment, enableTestEndpoints: enabled);
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/api/v1/test-error");
        await AssertProblem(response, HttpStatusCode.NotFound, "/api/v1/test-error");
    }

    [Fact]
    public async Task Diagnostic_exception_is_safe_and_server_recovers_on_same_host()
    {
        await using var factory = new ApiFactory(enableTestEndpoints: true);
        using var client = CreateClient(factory);
        using var error = await client.GetAsync("/api/v1/test-error");
        await AssertProblem(error, HttpStatusCode.InternalServerError, "/api/v1/test-error");
        var json = await error.Content.ReadAsStringAsync();
        Assert.DoesNotContain("POSTMAN_TEST_EXCEPTION", json);
        Assert.DoesNotContain("InvalidOperationException", json);

        using var health = await client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        var body = await health.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    private static async Task<JsonElement> AssertProblem(
        HttpResponseMessage response, HttpStatusCode status, string instance)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(instance, body.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        return body;
    }
}
