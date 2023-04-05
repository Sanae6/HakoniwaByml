using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using HakoniwaByml;
using HakoniwaByml.Common;
using HakoniwaByml.Iter;
using HakoniwaByml.Writer;

Memory<byte> data = File.ReadAllBytes(args[0]);
// BymlWriter moog = new BymlWriter(BymlDataType.Hash);
//
// moog.AddNull("Among");
// moog.Add("cutie", true);
// moog.Add("eh", 4.0f);
// moog.Add("bee", 3.0);
// moog["se"] = 23451L;
// moog.Add("su", 12345UL);
// moog.Add("おとこのこ", 231);
// moog.Add("thea", 125U);
// BymlArray city = new BymlArray();
// city.Add("c418");
// BymlHash mc2 = new BymlHash();
// mc2.Add("anadjasfa", 41489);
//
// moog.Add("ww", city);
// moog.Add("wx", mc2);
//
// data = moog.Serialize();
// File.WriteAllBytes("Moog.byml", data.ToArray());

BymlIter byml = new BymlIter(data);

StringBuilder Dump(BymlIter iter, string indent = "", StringBuilder? builder = null) {
    if (!iter.TryGetSize(out int size)) throw new Exception("what");

    builder ??= new StringBuilder();

    for (int i = 0; i < size; i++) {
        if (!iter.TryGetType(i, out BymlDataType type)) throw new Exception("How");
        if (!iter.TryGetKey(i, out string? key)) throw new Exception("when..");
        builder.Append($"{indent}[{type}]{key}: ");
        switch (type) {
            case BymlDataType.String: {
                if (!iter.TryGetValue(i, out string? value)) throw new Exception("...????");
                builder.AppendLine(value!.ToString(CultureInfo.InvariantCulture));
                break;
            }
            case BymlDataType.Bool: {
                if (!iter.TryGetValue(i, out bool value)) throw new Exception("...????");
                builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
                break;
            }
            case BymlDataType.Uint: {
                if (!iter.TryGetValue(i, out uint value)) throw new Exception("...????");
                builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
                break;
            }
            case BymlDataType.Int: {
                if (!iter.TryGetValue(i, out int value)) throw new Exception("...????");
                builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
                break;
            }
            case BymlDataType.Long: {
                if (!iter.TryGetValue(i, out long value)) throw new Exception("...????");
                builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
                break;
            }
            case BymlDataType.Ulong: {
                if (!iter.TryGetValue(i, out ulong value)) throw new Exception("...????");
                builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
                break;
            }
            case BymlDataType.Float: {
                if (!iter.TryGetValue(i, out float value)) throw new Exception("...????");
                builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
                break;
            }
            case BymlDataType.Double: {
                if (!iter.TryGetValue(i, out double value)) throw new Exception("...????");
                builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
                break;
            }
            case BymlDataType.Hash or BymlDataType.Array: {
                if (!iter.TryGetValue(i, out BymlIter sub)) throw new Exception("...????");
                builder.AppendLine();
                Dump(sub, indent + "  ", builder);
                break;
            }
            case BymlDataType.Null:
                builder.AppendLine("<null>");
                break;
            default:
                throw new NotImplementedException($"Cannot get {type}");
        }
    }

    return builder;
}

static bool VerifiHeader(BymlHeader header) {
    return header is {Tag: BymlHeader.LittleEndianMarker, Version: <= 3};
}

static bool VerifiStringTable(ReadOnlySpan<byte> span, out int end) {
    end = 0;

    int typeSize = MemoryMarshal.Read<int>(span);
    BymlDataType type = (BymlDataType) (typeSize & 0xFF);
    int size = (int) ((typeSize & 0xFFFFFF00) >> 8);

    if (type != BymlDataType.StringTable)
        return false;
    if (size < 1)
        return false;

    ReadOnlySpan<int> addressTable = MemoryMarshal.Cast<byte, int>(span[4..(8 + size * 4)]);
    end = addressTable[^1];

    for (int i = 1; i <= size; i++)
        if (span[addressTable[i] - 1] > 0)
            return false;

    for (int i = 0; i < size; i++)
        if (addressTable[i] >= addressTable[i + 1])
            return false;

    if (4 * (size + 2) != addressTable[0])
        return false;

    for (int i = 0; i < size - 1; i++) {
        if (Extensions.StringCompare(span[addressTable[i]..], span[addressTable[i + 1]..]) > 0)
            return false;
    }

    return true;
}

static bool VerifiIter(BymlIter iter) {
    if (!VerifiHeader(iter.Header))
        return false;

    int hashOffset = iter.Header.HashKeyTableOffset;
    int afterHashOffset = hashOffset;
    if (hashOffset > 0) {
        ReadOnlySpan<byte> tableSpan = iter.Data[hashOffset..];
        if (!VerifiStringTable(tableSpan, out afterHashOffset))
            return false;
        afterHashOffset += hashOffset;
    }

    int stringOffset = iter.Header.StringTableOffset;
    int afterStringOffset = stringOffset;
    if (stringOffset > 0) {
        ReadOnlySpan<byte> tableSpan = iter.Data[stringOffset..];
        if (!VerifiStringTable(tableSpan, out afterStringOffset))
            return false;
        afterStringOffset += stringOffset;
    }

    int rootOffset = iter.Header.DataOffset;

    return (hashOffset == 0 && stringOffset == 0 || rootOffset != 0)
           && (hashOffset == 0
               || (stringOffset == 0 || afterHashOffset <= stringOffset)
               && (rootOffset == 0 || afterHashOffset <= rootOffset))
           && (afterStringOffset <= rootOffset || stringOffset == 0 || rootOffset == 0);
}


Console.WriteLine($"Valid: {VerifiIter(byml)}");
BymlWriter reser = new BymlWriter(BymlWriter.Copy(byml));
data = reser.Serialize(byml.Version);

File.WriteAllBytes("Moog.byml", data.ToArray());
File.WriteAllText("Old.yml", Dump(byml).ToString());
byml = new BymlIter(data);
Console.WriteLine($"Reserialized: {VerifiIter(byml)}");
File.WriteAllText("New.yml", Dump(byml).ToString());
Console.WriteLine("Done!");
Console.ReadKey();