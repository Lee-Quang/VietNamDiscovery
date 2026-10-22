using VietnamDiscovery.Api.Services.Interfaces;
using VietnamDiscovery.Api.Services.Implementations;
using VietnamDiscovery.Api.Configurations;
using VietnamDiscovery.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ApiProblemDetails.Customize;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection
builder.Services.AddScoped<IHealthService, HealthService>();

var app = builder.Build();

// Keep the same safe error contract in Development and Production.
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    await ApiProblemDetails.CreateResult(context.HttpContext, context.HttpContext.Response.StatusCode)
        .ExecuteAsync(context.HttpContext);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Allows the integration tests to host the real application pipeline.
public partial class Program { }
