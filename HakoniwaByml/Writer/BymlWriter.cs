using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using HakoniwaByml.Common;
using HakoniwaByml.Iter;

namespace HakoniwaByml.Writer;

public sealed class BymlWriter {
    private int MaxHashLength;
    private int MaxStringLength;
    private readonly Dictionary<string, List<long>> HashKeys = new Dictionary<string, List<long>>();
    private readonly Dictionary<string, List<long>> Strings = new Dictionary<string, List<long>>();
    private readonly Dictionary<BymlContainer, int> ContainerOffsets = new Dictionary<BymlContainer, int>();
    public BymlContainer Root { get; }
    public BymlWriter(BymlDataType rootType) {
        Root = rootType switch {
            BymlDataType.Array => new BymlArray(),
            BymlDataType.Hash => new BymlHash(),
            _ => throw new ArgumentException("Root data type must be Array or Hash", nameof(rootType))
        };
    }
    public BymlWriter(BymlContainer container) {
        Root = container;
    }

    internal int AddHashString(string key, long position) {
        lock (HashKeys) {
            if (HashKeys.TryGetValue(key, out List<long>? list))
                list.Add(position);
            else {
                MaxHashLength = Math.Max(MaxHashLength, Encoding.UTF8.GetByteCount(key));
                HashKeys.Add(key, new List<long> {position});
            }
            return 0;
        }
    }

    internal int AddString(string value, long position) {
        lock (Strings) {
            if (Strings.TryGetValue(value, out List<long>? list))
                list.Add(position);
            else {
                MaxStringLength = Math.Max(MaxStringLength, Encoding.UTF8.GetByteCount(value));
                Strings.Add(value, new List<long> {position});
            }
            return 0;
        }
    }

    private static int SerializeStringTable(BinaryWriter writer, List<KeyValuePair<string, List<long>>> list,
        int maxSize, bool hash) {
        if (list.Count == 0)
            return 0;

        long addrStart = writer.BaseStream.Position;
        writer.Write(list.Count << 8 | (int) BymlDataType.StringTable);

        long offsetsPos = addrStart + 4;
        int offset = 4 * (list.Count + 2), i = 0;
        Span<byte> strBuffer = stackalloc byte[maxSize];
        Span<byte> hashSpan = stackalloc byte[8];
        foreach ((string? str, List<long>? longs) in list) {
            int size = Encoding.UTF8.GetBytes(str, strBuffer);
            writer.BaseStream.Position = addrStart + offset;
            writer.Write(strBuffer[..size]);
            writer.Write((byte) 0);

            writer.BaseStream.Position = offsetsPos;
            writer.Write(offset);
            offset += size + 1;
            offsetsPos += 4;

            foreach (long pos in longs) {
                writer.BaseStream.Position = pos;
                if (hash) {
                    _ = writer.BaseStream.Read(hashSpan);
                    writer.BaseStream.Position -= hashSpan.Length;
                    ref BymlHashPair pair = ref MemoryMarshal.AsRef<BymlHashPair>(hashSpan);
                    pair.Key = i;
                    writer.Write(ref pair);
                } else {
                    writer.Write(i);
                }
            }
            i++;
        }
        long l = writer.BaseStream.Position = addrStart + offset;
        l = (4 - (offset & 0b11));
        for (int j = 0; j < l; j++) {
            writer.Write((byte) 0);
        }

        return (int) addrStart;
    }

    public int SerializeContainer(BymlContainer container, BinaryWriter writer) {
        if (ContainerOffsets.TryGetValue(container, out int offset)) return offset;
        offset = container.Serialize(this, writer);
        ContainerOffsets.Add(container, offset);
        return offset;
    }

    public Memory<byte> Serialize(ushort version = 3) {
        ContainerOffsets.Clear();
        lock (Strings) {
            MaxStringLength = 0;
            Strings.Clear();
        }
        lock (HashKeys) {
            MaxHashLength = 0;
            HashKeys.Clear();
        }
        using MemoryStream stream = new MemoryStream {
            Position = 16
        };
        using BinaryWriter writer = new BinaryWriter(stream);

        BymlHeader header = new BymlHeader {
            Tag = BymlHeader.LittleEndianMarker,
            Version = version,
            DataOffset = Root.Serialize(this, writer)
        };

        lock (Strings) {
            List<KeyValuePair<string, List<long>>> strings = Strings.ToList();
            strings.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.Ordinal));
            header.StringTableOffset = SerializeStringTable(writer, strings, MaxStringLength, false);
        }
        lock (HashKeys) {
            List<KeyValuePair<string, List<long>>> hashKeys = HashKeys.ToList();
            hashKeys.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.Ordinal));
            header.HashKeyTableOffset = SerializeStringTable(writer, hashKeys, MaxHashLength, true);
        }

        stream.Position = 0;
        stream.Write(ref header);

        return stream.ToArray().AsMemory();
    }

    public object this[string key] {
        set => Root[key] = value;
    }

    #region Root node add methods

    public void AddNull() { Root.AddNull(); }
    public void Add(bool value) { Root.Add(value); }
    public void Add(int value) { Root.Add(value); }
    public void Add(uint value) { Root.Add(value); }
    public void Add(long value) { Root.Add(value); }
    public void Add(ulong value) { Root.Add(value); }
    public void Add(float value) { Root.Add(value); }
    public void Add(double value) { Root.Add(value); }
    public void Add(string value) { Root.Add(value); }
    public void Add(BymlArray value) { Root.Add(value); }
    public void Add(BymlHash value) { Root.Add(value); }
    public void AddNull(string key) { Root.AddNull(key); }
    public void Add(string key, bool value) { Root.Add(key, value); }
    public void Add(string key, int value) { Root.Add(key, value); }
    public void Add(string key, uint value) { Root.Add(key, value); }
    public void Add(string key, long value) { Root.Add(key, value); }
    public void Add(string key, ulong value) { Root.Add(key, value); }
    public void Add(string key, float value) { Root.Add(key, value); }
    public void Add(string key, double value) { Root.Add(key, value); }
    public void Add(string key, string value) { Root.Add(key, value); }
    public void Add(string key, BymlArray value) { Root.Add(key, value); }
    public void Add(string key, BymlHash value) { Root.Add(key, value); }
    public void Add(string key, BymlContainer value) { Root.Add(key, value); }

    #endregion

    public static BymlContainer Copy(BymlIter iter) {
        BymlContainer container = iter.Type switch {
            BymlDataType.Array => new BymlArray(),
            BymlDataType.Hash => new BymlHash(),
            _ => throw new ArgumentException("Root data type must be Array or Hash")
        };

        foreach ((string? key, object? value) in iter) {
            if (iter.Type == BymlDataType.Array) {
                switch (value) {
                    case bool b:
                        container.Add(b);
                        break;
                    case int i:
                        container.Add(i);
                        break;
                    case uint u:
                        container.Add(u);
                        break;
                    case float f:
                        container.Add(f);
                        break;
                    case long l:
                        container.Add(l);
                        break;
                    case ulong ul:
                        container.Add(ul);
                        break;
                    case double d:
                        container.Add(d);
                        break;
                    case string s:
                        container.Add(s);
                        break;
                    case BymlIter sub:
                        container.Add(Copy(sub));
                        break;
                    case null:
                        container.AddNull();
                        break;
                    default:
                        throw new ArgumentException($"Invalid type {value.GetType()}", nameof(iter));
                }
            } else {
                Debug.Assert(key != null, nameof(key) + " != null");
                switch (value) {
                    case bool b:
                        container.Add(key, b);
                        break;
                    case int i:
                        container.Add(key, i);
                        break;
                    case uint u:
                        container.Add(key, u);
                        break;
                    case float f:
                        container.Add(key, f);
                        break;
                    case long l:
                        container.Add(key, l);
                        break;
                    case ulong ul:
                        container.Add(key, ul);
                        break;
                    case double d:
                        container.Add(key, d);
                        break;
                    case string s:
                        container.Add(key, s);
                        break;
                    case BymlIter sub:
                        container.Add(key, Copy(sub));
                        break;
                    case null:
                        container.AddNull(key);
                        break;
                    default:
                        throw new ArgumentException($"Invalid type {value.GetType()}", nameof(iter));
                }
            }
        }

        return container;
    }
}