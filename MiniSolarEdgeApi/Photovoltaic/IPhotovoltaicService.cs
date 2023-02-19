namespace MiniSolarEdgeApi.Photovoltaic;

using System;

internal interface IPhotovoltaicService : IObservable<PhotovoltaicStatus>
{
    PhotovoltaicStatus? Status { get; }

    ValueTask RunAsync(CancellationToken cancellationToken = default);
}