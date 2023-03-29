using System.Diagnostics;
using System.Globalization;
using System.Text;
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

// byml.Where(x => x.Value is not BymlIter).ToDictionary(x => x.Key, x => x.Value);
// foreach ((string? key, object? value) in byml) {
//     Console.WriteLine($"{key} {value}");
// }

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
                builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
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
                if (type == BymlDataType.Array) return builder;
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

BymlContainer Reserialize(BymlIter iter) {
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
                    container.Add(Reserialize(sub));
                    break;
                case null:
                    container.AddNull();
                    break;
                default: throw new Exception("wwwwwfwsdmgkasd");
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
                    container.Add(key, Reserialize(sub));
                    break;
                case null:
                    container.AddNull(key);
                    break;
                default: throw new Exception("wwwwwfwsdmgkasd");
            }
        }
    }

    return container;
}

// Dump(byml);
BymlWriter reser = new BymlWriter(Reserialize(byml));
data = reser.Serialize(byml.Version);
// Dump(byml);
File.WriteAllText("Old.yml", Dump(byml).ToString());
File.WriteAllText("New.yml", Dump(new BymlIter(data)).ToString());
File.WriteAllBytes("Moog.byml", data.ToArray());
Console.WriteLine("Done!");
Console.ReadKey();