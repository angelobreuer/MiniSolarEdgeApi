using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MiniSolarEdgeApi.Modbus;
using MiniSolarEdgeApi.Photovoltaic;

var builder = WebApplication.CreateBuilder(args);

var endpoint = IPEndPoint.Parse(builder.Configuration.GetValue<string>("Endpoint")!);

builder.Services
    .AddHealthChecks()
    .AddCheck<PhotovoltaicHealthCheck>("Photovoltaic");

builder.Services.AddHostedService<PhotovoltaicServiceHost>();
builder.Services.AddSingleton<IPhotovoltaicService, PhotovoltaicService>();
builder.Services.AddSingleton<IModbusClient>(new ModbusClient(endpoint));

var app = builder.Build();

app.MapHealthChecks("/healthz", new HealthCheckOptions { AllowCachingResponses = false, });

app.MapGet("/api/v1/photovoltaic", (IPhotovoltaicService photovoltaicService) =>
{
    var status = photovoltaicService.Status;

    if (status is null)
    {
        return Results.Problem(
            detail: "The photovoltaic data is currently not available.",
            title: "Data temporarily not available.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Json(status);
});

app.Run();

file sealed class PhotovoltaicHealthCheck : IHealthCheck
{
    private readonly IPhotovoltaicService _photovoltaicService;

    public PhotovoltaicHealthCheck(IPhotovoltaicService photovoltaicService)
    {
        ArgumentNullException.ThrowIfNull(photovoltaicService);

        _photovoltaicService = photovoltaicService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        if (_photovoltaicService.Status is not null)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Querying status from photovoltaic is possible."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Querying status from photovoltaic failed."));
    }
}