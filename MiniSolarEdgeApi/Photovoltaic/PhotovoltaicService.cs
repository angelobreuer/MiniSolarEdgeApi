namespace MiniSolarEdgeApi.Photovoltaic;

using System.Collections.Immutable;
using System.Net.Sockets;
using MiniSolarEdgeApi.Modbus;

internal sealed class PhotovoltaicService : IObservable<PhotovoltaicStatus>, IPhotovoltaicService
{
    private readonly ILogger<PhotovoltaicService> _logger;
    private readonly IModbusClient _modbusClient;
    private readonly object _observersSyncRoot;
    private readonly ImmutableArray<ModbusRegister> _registers;
    private ImmutableList<IObserver<PhotovoltaicStatus>> _observers;

    public PhotovoltaicService(IModbusClient modbusClient, ILogger<PhotovoltaicService> logger)
    {
        ArgumentNullException.ThrowIfNull(modbusClient);
        ArgumentNullException.ThrowIfNull(logger);

        _modbusClient = modbusClient;
        _logger = logger;
        _registers = ImmutableArray.Create<ModbusRegister>(
            new(40083, 1),  // AC Power (in Watts)
            new(40084, 1),  // AC Power Scale Factor
            new(62852, 2),  // Battery Status (%)
            new(62854, 2)); // Battery Mode

        _observers = ImmutableList<IObserver<PhotovoltaicStatus>>.Empty;
        _observersSyncRoot = new object();

        _ = RunAsync();
    }

    public PhotovoltaicStatus? Status { get; private set; }
    public async ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await FetchAsync(cancellationToken).ConfigureAwait(false);

                await periodicTimer
                    .WaitForNextTickAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            foreach (var observer in _observers)
            {
                observer.OnError(exception);
            }

            return;
        }

        foreach (var observer in _observers)
        {
            observer.OnCompleted();
        }
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<PhotovoltaicStatus> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_observersSyncRoot)
        {
            _observers = _observers.Add(observer);
        }

        return new ObserverRegistration(this, observer);
    }

    internal void Unsubscribe(IObserver<PhotovoltaicStatus> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_observersSyncRoot)
        {
            _observers = _observers.Remove(observer);
        }
    }

    private async Task FetchAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connected = true;

        ImmutableArray<ModbusValue> registerValues;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                registerValues = await _modbusClient
                    .ReadAsync(_registers, cancellationToken)
                    .ConfigureAwait(false);

                await Task
                    .Delay(TimeSpan.FromSeconds(10), cancellationToken)
                    .ConfigureAwait(false);

                break;
            }
            catch (SocketException exception)
            {
                if (connected)
                {
                    connected = false;
                    _logger.LogError(exception, "Failed to query photovoltaic status: {Message}.", exception.Message);
                }

                Status = null;

                await Task
                    .Delay(TimeSpan.FromSeconds(30), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var power = registerValues[0].Get(registerValues[1]);
        var batteryPercentage = registerValues[3].CombineToSingle(registerValues[2]) / 100.0F;
        var batteryStatusValue = registerValues[4].GetAsUInt16();

        var batteryStatus = batteryStatusValue switch
        {
            4 => PhotovoltaicBatteryStatus.Discharging,
            6 or 10 => PhotovoltaicBatteryStatus.Idle,
            _ => PhotovoltaicBatteryStatus.Charging,
        };

        var status = new PhotovoltaicStatus(
            Power: power,
            Battery: new PhotovoltaicBatteryInformation(batteryPercentage, batteryStatus));

        Status = status;

        _logger.LogInformation(
            "Power: {Power} W, Battery Percentage: {BatteryPercentage}%, Battery Status: {BatteryStatus} ({BatteryStatusValue})",
            power, batteryPercentage * 100F, batteryStatus, batteryStatusValue);

        foreach (var observer in _observers)
        {
            observer.OnNext(status);
        }
    }
}

file sealed record class ObserverRegistration(PhotovoltaicService Observable, IObserver<PhotovoltaicStatus> Observer) : IDisposable
{
    public void Dispose()
    {
        Observable.Unsubscribe(Observer);
    }
}