namespace MiniSolarEdgeApi.Modbus;

using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;

internal sealed class ModbusClient : IModbusClient
{
    private readonly IPEndPoint _endPoint;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ModbusClient"/> class.
    /// </summary>
    /// <param name="endPoint">the endpoint to connect to.</param>
    /// <exception cref="ArgumentNullException">
    ///     thrown if the specified endpoint (<paramref name="endPoint"/>) is <see langword="null"/>
    /// </exception>
    public ModbusClient(IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        _endPoint = endPoint;
    }

    /// <summary>
    ///     Reads out the specified <paramref name="registers"/> asynchronously.
    /// </summary>
    /// <param name="registers">the registers to read.</param>
    /// <param name="cancellationToken">
    ///     a cancellation token (<see cref="CancellationToken"/>) used to propagate notification that the 
    ///     asynchronous operation should be canceled.
    /// </param>
    /// <exception cref="OperationCanceledException">
    ///     thrown if the cancellation token (<paramref name="cancellationToken"/>) has had cancellation requested.
    /// </exception>
    /// <returns>
    ///     a value task (<see cref="ValueTask{T}"/>) that represents the asynchronous operation. The task 
    ///     result is an immutable array (<see cref="ImmutableArray{T}"/>) containing the register values read.
    /// </returns>
    public async ValueTask<ImmutableArray<ModbusValue>> ReadAsync(ImmutableArray<ModbusRegister> registers, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        await socket
            .ConnectAsync(_endPoint)
            .ConfigureAwait(false);

        var payload = new byte[12 * registers.Length];
        var index = 0;
        var bytesToArrive = 0;

        foreach (var register in registers)
        {
            WriteHeader(
                span: payload.AsSpan(index * 12, 12),
                index: index++,
                address: register.Address,
                count: register.Count);

            bytesToArrive += register.Count * 2 + 9;
        }

        await socket
            .SendAsync(payload.AsMemory(), SocketFlags.None, cancellationToken)
            .ConfigureAwait(false);

        var values = ImmutableArray.CreateBuilder<ModbusValue>();
        var receiveBuffer = GC.AllocateUninitializedArray<byte>(bytesToArrive);
        var bytesReceived = 0;

        while (bytesReceived < receiveBuffer.Length)
        {
            bytesReceived += await socket
                .ReceiveAsync(receiveBuffer.AsMemory(bytesReceived), SocketFlags.None, cancellationToken)
                .ConfigureAwait(false);
        }

        var receiveMemory = receiveBuffer.AsMemory();

        while (!receiveMemory.IsEmpty)
        {
            var correlationId = BinaryPrimitives.ReadUInt16BigEndian(receiveMemory.Span);
            var unit = receiveMemory.Span[6];
            var function = receiveMemory.Span[7];
            var responseDataLength = receiveMemory.Span[8];

            var response = receiveMemory.Slice(9, responseDataLength);

            while (!response.IsEmpty)
            {
                values.Add(new ModbusValue(BinaryPrimitives.ReadUInt16BigEndian(response.Span)));
                response = response[2..];
            }

            var payloadLength = 9 + responseDataLength;
            receiveMemory = receiveMemory[payloadLength..];
        }

        return values.ToImmutable();
    }

    private static void WriteHeader(Span<byte> span, int index, ushort address, ushort count)
    {
        BinaryPrimitives.WriteUInt16BigEndian(span[0..2], (ushort)index++);
        span[5] = 6; // message size
        span[6] = 1; // unit
        span[7] = 3; // Function code (3 = Read holding register)
        BinaryPrimitives.WriteUInt16BigEndian(span[8..10], address);
        BinaryPrimitives.WriteUInt16BigEndian(span[10..12], count);
    }
}
