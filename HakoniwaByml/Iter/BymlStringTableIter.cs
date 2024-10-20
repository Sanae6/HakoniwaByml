using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace HakoniwaByml.Iter;

public ref struct BymlStringTableIter {
    public ReadOnlySpan<byte> Data;
    public int Size => (int) (BinaryPrimitives.ReadUInt32LittleEndian(Data) >> 8);
    public ReadOnlySpan<int> AddressTable => MemoryMarshal.Cast<byte, int>(Data[4..(4 + 4 * Size)]);

    public BymlStringTableIter(ReadOnlySpan<byte> data) {
        Data = data;
    }

    public string GetString(int index) {
        return Data.ReadString(AddressTable[index]);
    }
}