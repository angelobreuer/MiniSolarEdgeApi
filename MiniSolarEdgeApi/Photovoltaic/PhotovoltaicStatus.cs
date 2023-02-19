namespace MiniSolarEdgeApi.Photovoltaic;

public sealed record class PhotovoltaicStatus(
    double Power,
    PhotovoltaicBatteryInformation Battery);
