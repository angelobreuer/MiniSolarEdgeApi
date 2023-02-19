using System.Net;
using MiniSolarEdgeApi.Modbus;
using MiniSolarEdgeApi.Photovoltaic;

var builder = WebApplication.CreateBuilder(args);
var endpoint = IPEndPoint.Parse(builder.Configuration.GetValue<string>("Endpoint")!);

builder.Services.AddHostedService<PhotovoltaicServiceHost>();
builder.Services.AddSingleton<IPhotovoltaicService, PhotovoltaicService>();
builder.Services.AddSingleton<IModbusClient>(new ModbusClient(endpoint));

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