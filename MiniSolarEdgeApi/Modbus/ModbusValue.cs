namespace MiniSolarEdgeApi.Modbus;

using System;
using System.Buffers.Binary;

internal readonly struct ModbusValue
{
    private readonly ushort _value;

    public ModbusValue(ushort value) => _value = value;

    public ushort GetAsUInt16() => _value;

    public short GetAsInt16() => (short)_value;

    public float CombineToSingle(ModbusValue value2)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(span, _value);
        BinaryPrimitives.WriteUInt16BigEndian(span[2..], value2._value);
        return BinaryPrimitives.ReadSingleBigEndian(span);
    }

    public double Get(ModbusValue scaleFactorValue) => _value * Math.Pow(10, scaleFactorValue.GetAsInt16());
}
