# MiniSolarEdgeApi
Very simple HTTP API to query the SolarEdge ModBus

## Getting started

1. Build project
2. Put target endpoint in appsettings.json (for ModBus protocol)
3. Open browser at localhost:\<port>/api/v1/photovoltaic

```json
{
  "power": 9635, // Power Production (W)
  "battery": {
    "percentage": 0.85, // Battery Percentage (0.0 - 1.0)
    "status": 0 // (0 = Idle, 1 = Charging, 2 = Discharging)
  }
}
```
