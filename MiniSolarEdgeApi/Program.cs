using System.Net;
using MiniSolarEdgeApi.Modbus;
using MiniSolarEdgeApi.Photovoltaic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<PhotovoltaicServiceHost>();
builder.Services.AddSingleton<IPhotovoltaicService, PhotovoltaicService>();
builder.Services.AddSingleton<IModbusClient>(new ModbusClient(IPEndPoint.Parse("192.168.5.2:502")));

var app = builder.Build();

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