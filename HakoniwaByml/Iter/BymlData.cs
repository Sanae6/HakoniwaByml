using System.Runtime.InteropServices;

namespace HakoniwaByml.Iter;

public readonly record struct BymlData(BymlDataType Type, int DataOffset) {
    public bool TryGetBool(ref BymlIter iter, out bool value) {
        value = default;
        if (Type == BymlDataType.Bool) {
            value = MemoryMarshal.Read<bool>(iter.Data[DataOffset..]);
            return true;
        }

        return false;
    }

    public bool TryGetInt(ref BymlIter iter, out int value) {
        value = default;

        if (Type == BymlDataType.Int) {
            value = MemoryMarshal.Read<int>(iter.Data[DataOffset..]);
            return true;
        }

        return false;
    }

    public bool TryGetUint(ref BymlIter iter, out uint value) {
        value = default;

        if (Type == BymlDataType.Uint) {
            value = MemoryMarshal.Read<uint>(iter.Data[DataOffset..]);
            return true;
        }

        return false;
    }

    public bool TryGetLong(ref BymlIter iter, out long value) {
        value = default;

        if (Type == BymlDataType.Long) {
            value = MemoryMarshal.Read<long>(iter.Data[DataOffset..]);
            return true;
        }

        return false;
    }

    public bool TryGetUlong(ref BymlIter iter, out ulong value) {
        value = default;

        if (Type == BymlDataType.Ulong) {
            value = MemoryMarshal.Read<ulong>(iter.Data[DataOffset..]);
            return true;
        }

        return false;
    }

    public bool TryGetFloat(ref BymlIter iter, out float value) {
        value = default;

        if (Type == BymlDataType.Float) {
            value = MemoryMarshal.Read<float>(iter.Data[DataOffset..]);
            return true;
        }

        return false;
    }

    public bool TryGetDouble(ref BymlIter iter, out double value) {
        value = default;

        if (Type == BymlDataType.Double) {
            value = MemoryMarshal.Read<double>(iter.Data[DataOffset..]);
            return true;
        }

        return false;
    }

    public bool TryGetString(ref BymlIter iter, out string? value) {
        value = default;

        if (Type == BymlDataType.String) {
            BymlStringTableIter stringTable = new BymlStringTableIter(iter.Data[iter.Header.StringTableOffset..]);

            value = stringTable.GetString(MemoryMarshal.Read<int>(iter.Data[DataOffset..]));
            return true;
        }

        return false;
    }

    public bool TryGetIter(ref BymlIter iter, out BymlIter value) {
        value = default;

        if (Type is BymlDataType.Hash or BymlDataType.Array) {
            value = new BymlIter(iter, DataOffset);
            return true;
        }

        return false;
    }
}