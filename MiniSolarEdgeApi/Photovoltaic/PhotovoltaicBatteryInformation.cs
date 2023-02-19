namespace MiniSolarEdgeApi.Photovoltaic;

public sealed record class PhotovoltaicBatteryInformation(
    float Percentage,
    PhotovoltaicBatteryStatus Status);