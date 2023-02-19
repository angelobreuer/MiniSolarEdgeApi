namespace MiniSolarEdgeApi.Photovoltaic;
internal sealed class PhotovoltaicServiceHost : BackgroundService
{
    private readonly IPhotovoltaicService _photovoltaicService;

    public PhotovoltaicServiceHost(IPhotovoltaicService photovoltaicService)
    {
        ArgumentNullException.ThrowIfNull(photovoltaicService);

        _photovoltaicService = photovoltaicService;
    }

    /// <inheritdoc/>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _photovoltaicService.RunAsync(stoppingToken).AsTask();
    }
}
