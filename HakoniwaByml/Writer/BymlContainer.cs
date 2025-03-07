﻿using System.Buffers.Binary;
using System.Text;
using HakoniwaByml.Common;
using HakoniwaByml.Iter;

namespace HakoniwaByml.Writer;

public abstract class BymlContainer {
    internal abstract int Serialize(BymlWriter owner, BinaryWriter writer);

    public virtual void AddNull() {
        throw new NotSupportedException();
    }

    public virtual void Add(bool value) {
        throw new NotSupportedException();
    }

    public virtual void Add(int value) {
        throw new NotSupportedException();
    }

    public virtual void Add(uint value) {
        throw new NotSupportedException();
    }

    public virtual void Add(long value) {
        throw new NotSupportedException();
    }

    public virtual void Add(ulong value) {
        throw new NotSupportedException();
    }

    public virtual void Add(float value) {
        throw new NotSupportedException();
    }

    public virtual void Add(double value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string value) {
        throw new NotSupportedException();
    }

    public virtual void Add(BymlArray value) {
        throw new NotSupportedException();
    }

    public virtual void Add(BymlHash value) {
        throw new NotSupportedException();
    }

    public virtual void Add(BymlContainer value) {
        throw new NotSupportedException();
    }

    public virtual void AddNull(string key) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, bool value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, int value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, uint value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, long value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, ulong value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, float value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, double value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, string value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, BymlArray value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, BymlHash value) {
        throw new NotSupportedException();
    }

    public virtual void Add(string key, BymlContainer value) {
        throw new NotSupportedException();
    }

    public virtual int Length() => throw new NotImplementedException();

    public virtual object this[string key] {
        set => throw new NotSupportedException();
    }

    public virtual string ToYaml(int depth = 0)
    {
        throw new NotSupportedException();
    }
}

public sealed class BymlArray : BymlContainer {
    private readonly List<Entry> Nodes = new List<Entry>();

    internal override int Serialize(BymlWriter owner, BinaryWriter writer) {
        long basePos = writer.BaseStream.Position;
        long typePos = basePos + 4,
            nodePos = (basePos + 4 + Nodes.Count).Align(0b11),
            dataPos = nodePos + Nodes.Count * 4;

        Span<byte> header = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, Nodes.Count << 8 | (int)BymlDataType.Array);
        writer.Write(header);

        foreach ((BymlDataType type, object? value) in Nodes) {
            writer.BaseStream.Position = typePos++;
            writer.Write((byte)type);
            long dataOffset = dataPos;
            bool wroteData = true;
            writer.BaseStream.Position = dataPos;
            switch (value) {
                case long l:
                    writer.Write(l);
                    dataPos += 8;
                    break;
                case ulong u:
                    writer.Write(u);
                    dataPos += 8;
                    break;
                case double d:
                    writer.Write(d);
                    dataPos += 8;
                    break;
                case BymlContainer c:
                    var (offset, written) = owner.SerializeContainer(c, writer);
                    dataOffset = offset;
                    if (written) {
                        dataPos += writer.BaseStream.Position - dataPos;
                    }

                    break;
                default:
                    wroteData = false;
                    break;
            }

            writer.BaseStream.Position = nodePos;
            nodePos += 4;
            if (wroteData) {
                owner.AddFixup(writer.BaseStream.Position, (int)dataOffset);
                writer.Write((int)dataOffset);
            }
            else {
                switch (value) {
                    case int i:
                        writer.Write(i);
                        break;
                    case uint u:
                        writer.Write(u);
                        break;
                    case float f:
                        writer.Write(f);
                        break;
                    case bool b:
                        writer.Write(b);
                        break;
                    case string s:
                        writer.Write(owner.AddString(s, writer.BaseStream.Position));
                        break;
                    default:
                        writer.Write(0);
                        break;
                }
            }
        }

        // set stream pos to end of node
        writer.BaseStream.Position = dataPos;
        return (int)basePos;
    }

    public override void AddNull() {
        Nodes.Add(new Entry(BymlDataType.Null, null!));
    }

    public override void Add(bool value) {
        Nodes.Add(new Entry(BymlDataType.Bool, value));
    }

    public override void Add(int value) {
        Nodes.Add(new Entry(BymlDataType.Int, value));
    }

    public override void Add(uint value) {
        Nodes.Add(new Entry(BymlDataType.Uint, value));
    }

    public override void Add(long value) {
        Nodes.Add(new Entry(BymlDataType.Long, value));
    }

    public override void Add(ulong value) {
        Nodes.Add(new Entry(BymlDataType.Ulong, value));
    }

    public override void Add(float value) {
        Nodes.Add(new Entry(BymlDataType.Float, value));
    }

    public override void Add(double value) {
        Nodes.Add(new Entry(BymlDataType.Double, value));
    }

    public override void Add(string value) {
        Nodes.Add(new Entry(BymlDataType.String, value));
    }

    public override void Add(BymlArray value) {
        Nodes.Add(new Entry(BymlDataType.Array, value));
    }

    public override void Add(BymlHash value) {
        Nodes.Add(new Entry(BymlDataType.Hash, value));
    }

    public override void Add(BymlContainer value) {
        Nodes.Add(new Entry(value switch {
            BymlHash => BymlDataType.Hash,
            BymlArray => BymlDataType.Array,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        }, value));
    }

    public override bool Equals(object? obj) {
        if (obj is not BymlArray array) return false;
        if (Nodes.Count != array.Nodes.Count) return false;
        foreach (((BymlDataType leftType, object? leftValue), (BymlDataType rightType, object? rightValue)) in
                 Nodes.Zip(array.Nodes)) {
            if (leftType != rightType) return false;
            if (!(leftValue?.Equals(rightValue) ?? false)) return false;
        }

        return true;
    }

    public void AddObject(object? value)
    {
        Nodes.Add(new Entry(value switch
        {
            bool => BymlDataType.Bool,
            int => BymlDataType.Int,
            uint => BymlDataType.Uint,
            long => BymlDataType.Long,
            ulong => BymlDataType.Ulong,
            float => BymlDataType.Float,
            double => BymlDataType.Double,
            string => BymlDataType.String,
            BymlHash => BymlDataType.Hash,
            BymlArray => BymlDataType.Array,
            _ => throw new ArgumentException("Invalid type passed.", nameof(value))
        }, value));
    }

    public override int GetHashCode()
    {
        HashCode result = new HashCode();

        foreach (var node in Nodes)
            result.Add(HashCode.Combine(node.Key, node.Value));

        return result.ToHashCode();
    }

    public override string ToYaml(int depth = 0)
    {
        StringBuilder result = new StringBuilder();
        int idx = 0;
        foreach (var entry in Nodes)
        {
            var label = $"{idx++}: ";
            result.Append(label.PadLeft(label.Length + depth, '\t'));
            if (entry.Value is BymlContainer container)
                result.Append($"\n{container.ToYaml(depth + 1)}");
            else if (entry.Key == BymlDataType.Null)
                result.AppendLine("null");
            else
                result.AppendLine(entry.Value.ToString());
        }
        return result.ToString();
    }

    public override int Length() => Nodes.Count;

    private record struct Entry(BymlDataType Key, object? Value);
}

public sealed class BymlHash : BymlContainer {
    private readonly SortedSet<Entry> Nodes = new SortedSet<Entry>(new EntryComparer());

    internal override int Serialize(BymlWriter owner, BinaryWriter writer) {
        long basePos = writer.BaseStream.Position;
        long entryPos = basePos + 4,
            dataPos = (entryPos + Nodes.Count * 8).Align(0b11) + Nodes.Count * 4;

        writer.Write(Nodes.Count << 8 | (int)BymlDataType.Hash);

        foreach ((BymlDataType type, string name, object? value) in Nodes) {
            BymlHashPair pair = new BymlHashPair {
                Type = type,
                Key = owner.AddHashString(name, entryPos),
                Value = (int)dataPos
            };
            writer.BaseStream.Position = dataPos;
            switch (value) {
                case long l:
                    writer.Write(l);
                    owner.AddFixup(entryPos + 4, (int)dataPos);
                    dataPos += 8;
                    break;
                case ulong u:
                    writer.Write(u);
                    owner.AddFixup(entryPos + 4, (int)dataPos);
                    dataPos += 8;
                    break;
                case double d:
                    writer.Write(d);
                    owner.AddFixup(entryPos + 4, (int)dataPos);
                    dataPos += 8;
                    break;
                case BymlContainer c:
                    var (offset, written) = owner.SerializeContainer(c, writer);
                    pair.Value = offset;
                    owner.AddFixup(entryPos + 4, offset);
                    if (written) {
                        dataPos = writer.BaseStream.Position;
                    }

                    break;
                case int i:
                    pair.Value = i;
                    break;
                case uint u:
                    pair.Value = (int)u;
                    break;
                case float f:
                    pair.Value = BitConverter.SingleToInt32Bits(f);
                    break;
                case bool b:
                    pair.Value = Convert.ToInt32(b);
                    break;
                case string s:
                    pair.Value = owner.AddString(s, entryPos + 4);
                    break;
            }

            writer.BaseStream.Position = entryPos;
            writer.Write(ref pair);
            entryPos += 8;
        }

        // set stream pos to end of node
        writer.BaseStream.Position = dataPos;
        return (int)basePos;
    }

    private void Add(BymlDataType type, string name, object data) {
        if (!Nodes.Add(new Entry(type, name, data)))
            throw new ArgumentException("A node with the same key already exists.", nameof(name));
    }

    public override void AddNull(string name) {
        Add(BymlDataType.Null, name, null!);
    }

    public override void Add(string name, bool value) {
        Add(BymlDataType.Bool, name, value);
    }

    public override void Add(string name, int value) {
        Add(BymlDataType.Int, name, value);
    }

    public override void Add(string name, uint value) {
        Add(BymlDataType.Uint, name, value);
    }

    public override void Add(string name, long value) {
        Add(BymlDataType.Long, name, value);
    }

    public override void Add(string name, ulong value) {
        Add(BymlDataType.Ulong, name, value);
    }

    public override void Add(string name, float value) {
        Add(BymlDataType.Float, name, value);
    }

    public override void Add(string name, double value) {
        Add(BymlDataType.Double, name, value);
    }

    public override void Add(string name, string value) {
        Add(BymlDataType.String, name, value);
    }

    public override void Add(string name, BymlArray value) {
        Add(BymlDataType.Array, name, value);
    }

    public override void Add(string name, BymlHash value) {
        Add(BymlDataType.Hash, name, value);
    }

    public override void Add(string name, BymlContainer value) {
        Add(value switch {
            BymlHash => BymlDataType.Hash,
            BymlArray => BymlDataType.Array,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        }, name, value);
    }

    public override bool Equals(object? obj) {
        if (obj is not BymlHash hash) return false;
        if (!Nodes.SetEquals(hash.Nodes)) return false;
        foreach (((_, _, object? leftValue), (_, _, object? rightValue)) in Nodes.Zip(hash.Nodes)) {
            if (!(leftValue?.Equals(rightValue) ?? false)) return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        HashCode result = new HashCode();

        foreach (var node in Nodes)
            result.Add(HashCode.Combine(node.Type, node.Name, node.Value));

        return result.ToHashCode();
    }
    
    public override int Length() => Nodes.Count;

    public override object this[string key] {
        set => Add(value switch {
            bool => BymlDataType.Bool,
            int => BymlDataType.Int,
            uint => BymlDataType.Uint,
            long => BymlDataType.Long,
            ulong => BymlDataType.Ulong,
            float => BymlDataType.Float,
            double => BymlDataType.Double,
            string => BymlDataType.String,
            null => BymlDataType.Null,
            BymlHash => BymlDataType.Hash,
            BymlArray => BymlDataType.Array,
            _ => throw new ArgumentException("Invalid type passed.", nameof(value))
        }, key, value!);
    }

    public override string ToYaml(int depth = 0)
    {
        StringBuilder result = new StringBuilder();
        foreach (var entry in Nodes)
        {
            var label = $"{entry.Name}: ";
            result.Append(label.PadLeft(label.Length + depth, '\t'));
            if (entry.Value is BymlContainer container)
                result.Append($"\n{container.ToYaml(depth + 1)}");
            else if (entry.Type == BymlDataType.Null)
                result.AppendLine("null");
            else
                result.AppendLine(entry.Value.ToString());
        }
        return result.ToString();
    }

    private record struct Entry(BymlDataType Type, string Name, object Value);

    private class EntryComparer : Comparer<Entry> {
        public override int Compare(Entry x, Entry y) {
            return string.CompareOrdinal(x.Name, y.Name);
        }
    }
}