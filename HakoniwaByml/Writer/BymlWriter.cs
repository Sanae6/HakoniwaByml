using System.Buffers.Binary;
using HakoniwaByml.Common;
using HakoniwaByml.Iter;

namespace HakoniwaByml.Writer;

// TODO: Add constructor that takes in IEnumerable and converts it cleanly to BymlWriter equivalents
public sealed class BymlWriter {
    private readonly LinkedList<string> HashKeys = new LinkedList<string>();
    private readonly LinkedList<string> Strings = new LinkedList<string>();
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

    internal int AddHashString(string key) {
        lock (HashKeys) {
            LinkedListNode<string>? cur = HashKeys.First;
            int i = 0;
            while (cur != null && cur.Next != HashKeys.First) {
                if (cur.Value.Equals(key))
                    return i;
                i++;
                cur = cur.Next;
            }

            if (cur == null) HashKeys.AddLast(key);
            else HashKeys.AddAfter(cur, key);
            return i;
        }
    }

    internal int AddString(string value) {
        lock (Strings) {
            LinkedListNode<string>? cur = Strings.First;
            int i = 0;
            while (cur != null && cur.Next != Strings.Last) {
                if (cur.Value.Equals(value))
                    return i;
                i++;
                cur = cur.Next;
            }

            if (cur == null) Strings.AddLast(value);
            else Strings.AddAfter(cur, value);
            return i;
        }
    }

    private static int SerializeStringTable(BinaryWriter writer, LinkedList<string> list) {
        if (list.Count == 0)
            return 0;

        long addrStart = writer.BaseStream.Position;
        Span<byte> header = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, list.Count << 8 | (int) BymlDataType.StringTable);
        writer.Write(header);

        Span<int> offsets = stackalloc int[list.Count + 1];
        writer.BaseStream.Position += list.Count * 4 + 4;
        writer.BaseStream.Position = writer.BaseStream.Position.Align(0b11);

        int i = 0;
        foreach (string str in list) {
            long offset = writer.BaseStream.Position - addrStart;
            writer.Write(str.AsSpan());
            writer.Write((byte) 0);
            offsets[i++] = (int) offset;
        }

        offsets[list.Count] = (int) (writer.BaseStream.Position - addrStart);

        long end = writer.BaseStream.Position;
        writer.BaseStream.Position = addrStart + 4;
        foreach (int offset in offsets) {
            writer.Write(offset);
        }

        writer.BaseStream.Position = end;

        return (int) addrStart;
    }

    public int SerializeContainer(BymlContainer container, BinaryWriter writer) {\
        if (ContainerOffsets.TryGetValue(container, out int offset)) return offset;
        offset = container.Serialize(this, writer);
        ContainerOffsets.Add(container, offset);
        return offset;
    }

    public Memory<byte> Serialize(ushort version = 3) {
        ContainerOffsets.Clear();
        using MemoryStream stream = new MemoryStream {
            Position = 16
        };
        using BinaryWriter writer = new BinaryWriter(stream);

        BymlHeader header = new BymlHeader {
            Tag = BymlHeader.LittleEndianMarker,
            Version = version,
            DataOffset = Root.Serialize(this, writer)
        };

        lock (HashKeys) {
            header.HashKeyTableOffset = SerializeStringTable(writer, HashKeys);
        }
        lock (Strings) {
            header.StringTableOffset = SerializeStringTable(writer, Strings);
        }

        stream.Position = 0;
        stream.Write(ref header);

        return stream.ToArray().AsMemory();
    }

    public object this[string key] {
        set => Root[key] = value;
    }

    #region Wrapping Adders
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
    public void Add(BymlContainer value) { Root.Add(value); }
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
}