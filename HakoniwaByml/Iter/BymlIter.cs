﻿using System.Buffers.Binary;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HakoniwaByml.Common;
using HakoniwaByml.Writer;

namespace HakoniwaByml.Iter;

public record struct BymlIter : IEnumerable<KeyValuePair<string?, object?>> {
    private readonly ReadOnlyMemory<byte> Buffer;
    public readonly BymlDataType Type;
    internal BymlHeader Header;
    internal int RootNode;
    private bool LittleEndian => Header.Tag == BymlHeader.LittleEndianMarker; // YB
    public ushort Version => Header.Version;
    internal ReadOnlySpan<byte> Data => Buffer.Span;
    public bool Iterable => Type is BymlDataType.Array or BymlDataType.Hash;

    public BymlIter(ReadOnlyMemory<byte> data) {
        Buffer = data;
        Header = MemoryMarshal.Read<BymlHeader>(Buffer.Span);
        if (!LittleEndian) throw new Exception("Invalid Header Tag! File is either big endian or not a BYML!");

        RootNode = Header.DataOffset;
        Type = Header.DataOffset == 0 && Header.HashKeyTableOffset == 0 && Header.StringTableOffset == 0 ? BymlDataType.Null : (BymlDataType)data.Span[RootNode];
    }

    internal BymlIter(BymlIter owner, int rootNode) {
        Buffer = owner.Buffer;
        Header = owner.Header;
        RootNode = rootNode;
        Type = (BymlDataType) owner.Data[RootNode];
    }

    public bool TryGetSize(out int size) {
        size = 0; // explicit zero instead of default
        if (RootNode == 0) return false;
        if (Type is BymlDataType.Array or BymlDataType.Hash) {
            size = BinaryPrimitives.ReadInt32LittleEndian(Data[RootNode..]) >> 8;
            return true;
        }
        return false;
    }

    public bool TryGetKey(int index, out string? key) {
        key = default;
        if (RootNode == 0) return false;

        if (Type == BymlDataType.Array) {
            key = index.ToString();
            return true;
        }

        if (Type == BymlDataType.Hash) {
            int size = BinaryPrimitives.ReadInt32LittleEndian(Data[RootNode..]) >> 8;
            ReadOnlySpan<BymlHashPair> pairs =
                MemoryMarshal.Cast<byte, BymlHashPair>(Data[(RootNode + 4)..(RootNode + 4 + size * 8)]);
            BymlStringTableIter hashTable = new BymlStringTableIter(Data[Header.HashKeyTableOffset..]);
            key = hashTable.GetString(pairs[index].Key);

            return true;
        }

        return false;
    }

    public bool ContainsKey(string key) {
        if (RootNode == 0) return false;

        if (Type == BymlDataType.Hash) {
            int size = BinaryPrimitives.ReadInt32LittleEndian(Data[RootNode..]) >> 8;
            ReadOnlySpan<BymlHashPair> pairs =
                MemoryMarshal.Cast<byte, BymlHashPair>(Data[(RootNode + 4)..(RootNode + 4 + size * 8)]);
            BymlStringTableIter hashTable = new BymlStringTableIter(Data[Header.HashKeyTableOffset..]);

            int low = 0;
            int high = size;
            while (low < high) {
                int avg = (low + high) / 2;
                BymlHashPair pair = pairs[avg];
                int result = string.Compare(key, hashTable.GetString(pair.Key),
                    StringComparison.Ordinal);
                switch (result) {
                    case 0:
                        return true;
                    case > 0:
                        low = avg + 1;
                        break;
                    case < 0:
                        high = avg;
                        break;
                }
            }

            return false;
        }
        return false;
    }

    internal bool TryGetValue(int index, out BymlData data) {
        data = default;
        if (RootNode == 0 || index < 0)
            return false;

        if (Type == BymlDataType.Array) {
            int size = BinaryPrimitives.ReadInt32LittleEndian(Data[RootNode..]) >> 8;
            if (size <= index) return false;
            int typeOffset = RootNode + 4 + index;
            int dataOffset = RootNode + 4 + size.Align(0b11) + 4 * index;
            BymlDataType type = (BymlDataType) Data[typeOffset];
            data = new BymlData(type,
                type.IsRegularValue() ? dataOffset : BinaryPrimitives.ReadInt32LittleEndian(Data[dataOffset..]));
            return true;
        }

        if (Type == BymlDataType.Hash) {
            int size = BinaryPrimitives.ReadInt32LittleEndian(Data[RootNode..]) >> 8;
            if (size <= index) return false;
            ReadOnlySpan<BymlHashPair> pairs =
                MemoryMarshal.Cast<byte, BymlHashPair>(Data[(RootNode + 4)..(RootNode + 4 + size * 8)]);
            BymlHashPair pair = pairs[index];
            int dataOffset = RootNode + 4 + index * 8 + 4;
            data = new BymlData(pair.Type,
                pair.Type.IsRegularValue() ? dataOffset : BinaryPrimitives.ReadInt32LittleEndian(Data[dataOffset..]));

            return true;
        }

        return false;
    }

    internal bool TryGetValue(string key, out BymlData data) {
        data = default;

        if (Type == BymlDataType.Hash) {
            int size = BinaryPrimitives.ReadInt32LittleEndian(Data[RootNode..]) >> 8;
            ReadOnlySpan<BymlHashPair> pairs =
                MemoryMarshal.Cast<byte, BymlHashPair>(Data[(RootNode + 4)..(RootNode + 4 + size * 8)]);
            BymlStringTableIter hashTable = new BymlStringTableIter(Data[Header.HashKeyTableOffset..]);

            int low = 0;
            int high = size;
            while (low < high) {
                int avg = (low + high) / 2;
                BymlHashPair pair = pairs[avg];
                int result = string.Compare(key, hashTable.GetString(pair.Key),
                    StringComparison.Ordinal);
                switch (result) {
                    case 0:
                        int dataOffset = RootNode + 4 + avg * 8 + 4;
                        data = new BymlData(pair.Type,
                            pair.Type.IsRegularValue()
                                ? dataOffset
                                : BinaryPrimitives.ReadInt32LittleEndian(Data[dataOffset..]));
                        return true;
                    case > 0:
                        low = avg + 1;
                        break;
                    case < 0:
                        high = avg;
                        break;
                }
            }
            return true;
        }

        return false;
    }

    public bool TryGetType(int index, out BymlDataType type) {
        type = default;
        if (TryGetValue(index, out BymlData data)) {
            type = data.Type;
            return true;
        }
        return false;
    }

    public bool TryGetType(string key, out BymlDataType type) {
        type = default;

        if (TryGetValue(key, out BymlData data)) {
            type = data.Type;
            return true;
        }

        return false;
    }

    public bool TryGetValue(int index, out bool value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetBool(ref this, out value);
    }

    public bool TryGetValue(string key, out bool value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetBool(ref this, out value);
    }

    public bool TryGetValue(int index, out int value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetInt(ref this, out value);
    }

    public bool TryGetValue(string key, out int value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetInt(ref this, out value);
    }

    public bool TryGetValue(int index, out uint value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetUint(ref this, out value);
    }

    public bool TryGetValue(string key, out uint value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetUint(ref this, out value);
    }

    public bool TryGetValue(int index, out float value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetFloat(ref this, out value);
    }

    public bool TryGetValue(string key, out float value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetFloat(ref this, out value);
    }

    public bool TryGetValue(int index, out long value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetLong(ref this, out value);
    }

    public bool TryGetValue(string key, out long value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetLong(ref this, out value);
    }

    public bool TryGetValue(int index, out ulong value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetUlong(ref this, out value);
    }

    public bool TryGetValue(string key, out ulong value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetUlong(ref this, out value);
    }

    public bool TryGetValue(int index, out double value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetDouble(ref this, out value);
    }

    public bool TryGetValue(string key, out double value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetDouble(ref this, out value);
    }

    public bool TryGetValue(int index, out string? value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetString(ref this, out value);
    }

    public bool TryGetValue(string key, out string? value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetString(ref this, out value);
    }

    public bool TryGetValue(int index, out BymlIter value) {
        value = default;
        return TryGetValue(index, out BymlData data) && data.TryGetIter(ref this, out value);
    }

    public bool TryGetValue(string key, out BymlIter value) {
        value = default;
        return TryGetValue(key, out BymlData data) && data.TryGetIter(ref this, out value);
    }

    #region Helpers
    public bool TryGetValue(int index, out object? value) {
        value = null!;
        if (!TryGetValue(index, out BymlData data)) {
            return false;
        }

        switch (data.Type) {
            case BymlDataType.Bool:
                if (TryGetValue(index, out bool b)) {
                    value = b;
                    return true;
                }
                break;
            case BymlDataType.Int:
                if (TryGetValue(index, out int i)) {
                    value = i;
                    return true;
                }
                break;
            case BymlDataType.Uint:
                if (TryGetValue(index, out uint u)) {
                    value = u;
                    return true;
                }
                break;
            case BymlDataType.Long:
                if (TryGetValue(index, out long l)) {
                    value = l;
                    return true;
                }
                break;
            case BymlDataType.Ulong:
                if (TryGetValue(index, out ulong ul)) {
                    value = ul;
                    return true;
                }
                break;
            case BymlDataType.Float:
                if (TryGetValue(index, out float f)) {
                    value = f;
                    return true;
                }
                break;
            case BymlDataType.Double:
                if (TryGetValue(index, out double d)) {
                    value = d;
                    return true;
                }
                break;
            case BymlDataType.String:
                if (TryGetValue(index, out string? s)) {
                    value = s;
                    return true;
                }
                break;
            case BymlDataType.Hash or BymlDataType.Array:
                if (TryGetValue(index, out BymlIter t)) {
                    value = t;
                    return true;
                }
                break;
            case BymlDataType.Null:
                return true;
        }

        return false;
    }

    public bool TryGetValue(string key, out object? value) {
        value = null!;
        if (!TryGetValue(key, out BymlData data)) {
            return false;
        }

        switch (data.Type) {
            case BymlDataType.Bool:
                if (TryGetValue(key, out bool b)) {
                    value = b;
                    return true;
                }
                break;
            case BymlDataType.Int:
                if (TryGetValue(key, out int i)) {
                    value = i;
                    return true;
                }
                break;
            case BymlDataType.Uint:
                if (TryGetValue(key, out uint u)) {
                    value = u;
                    return true;
                }
                break;
            case BymlDataType.Long:
                if (TryGetValue(key, out long l)) {
                    value = l;
                    return true;
                }
                break;
            case BymlDataType.Ulong:
                if (TryGetValue(key, out ulong ul)) {
                    value = ul;
                    return true;
                }
                break;
            case BymlDataType.Float:
                if (TryGetValue(key, out float f)) {
                    value = f;
                    return true;
                }
                break;
            case BymlDataType.Double:
                if (TryGetValue(key, out double d)) {
                    value = d;
                    return true;
                }
                break;
            case BymlDataType.String:
                if (TryGetValue(key, out string? s)) {
                    value = s;
                    return true;
                }
                break;
            case BymlDataType.Hash or BymlDataType.Array:
                if (TryGetValue(key, out BymlIter t)) {
                    value = t;
                    return true;
                }
                break;
            case BymlDataType.Null:
                return true;
        }

        return false;
    }

    /**
     * <remarks>May have a performance penalty, it's best to cache the returned size.</remarks>
     */
    public int GetSize() {
        if (!Iterable) throw new InvalidCastException("This node is not iterable.");
        TryGetSize(out int size);
        return size;
    }
    public object? this[int index] {
        get {
            if (!Iterable)
                throw new InvalidCastException("This node is not iterable.");
            if (!TryGetValue(index, out object? value))
                throw new ArgumentOutOfRangeException(nameof(index));
            return value;
        }
    }

    public object? this[string key]
    {
        get
        {
            if (!Iterable)
                throw new InvalidCastException("This node is not iterable.");
            if (!TryGetValue(key, out object? value))
                throw new ArgumentOutOfRangeException(nameof(key));
            return value;
        }
    }

    public IEnumerable<KeyValuePair<string?, T?>> As<T>() {
        return this.Select(pair => new KeyValuePair<string?, T?>(pair.Key, pair.Value != null ? (T?) pair.Value : default(T)));
    }

    public IEnumerable<T?> AsArray<T>() {
        return this.Select(pair => pair.Value != null ? (T?)pair.Value : default(T));
    }

    IEnumerator<KeyValuePair<string?, object?>> IEnumerable<KeyValuePair<string?, object?>>.GetEnumerator() {
        int length = GetSize();
        if (!Iterable) throw new InvalidCastException("This node is not iterable.");
        for (int i = 0; i < length; i++) {
            TryGetKey(i, out string? key);
            TryGetValue(i, out object? value);

            yield return new KeyValuePair<string?, object?>(key, value);
        }
    }
    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable<KeyValuePair<string, object?>>) this).GetEnumerator();
    }

    public string ToYaml(int indentLevel = 0)
    {
        StringBuilder result = new StringBuilder();

        foreach ((var key, var value) in this)
        {
            var label = $"{key}: ";
            result.Append(label.PadLeft(label.Length + indentLevel, '\t'));

            if (value is null)
                result.AppendLine("null");
            else if (value is BymlIter iter)
                result.Append($"\n{iter.ToYaml(indentLevel+1)}");
            else
                result.AppendLine(value.ToString());
        }

        return result.ToString();
    }

    public static bool IsValid(ReadOnlySpan<byte> data)
    {
        return data.Length > Unsafe.SizeOf<BymlHeader>() && MemoryMarshal.Read<BymlHeader>(data).Tag == BymlHeader.LittleEndianMarker;
    }

    #endregion
}