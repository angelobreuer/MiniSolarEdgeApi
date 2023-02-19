namespace MiniSolarEdgeApi.Modbus;

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

internal interface IModbusClient
{
    ValueTask<ImmutableArray<ModbusValue>> ReadAsync(ImmutableArray<ModbusRegister> registers, CancellationToken cancellationToken = default);
}