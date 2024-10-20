using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HakoniwaByml.Iter;

namespace HakoniwaByml;

public static class Extensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Align(this int value, int mask) {
        return value + mask & ~mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long Align(this long value, int mask) {
        return value + mask & ~mask;
    }

    public static bool IsRegularValue(this BymlDataType type) {
        return type is BymlDataType.Bool or BymlDataType.Int or BymlDataType.Uint or BymlDataType.Float
            or BymlDataType.String or BymlDataType.Null;
    }
    internal static void Write<T>(this Stream stream, ref T value) where T : struct {
        Span<byte> data = stackalloc byte[Unsafe.SizeOf<T>()];
        MemoryMarshal.Write(data, ref value);
        stream.Write(data);
    }
    internal static void Write<T>(this BinaryWriter writer, ref T value) where T : struct {
        writer.BaseStream.Write(ref value);
    }

    internal static string ReadString(this ReadOnlySpan<byte> data, int start) {
        int end = start;
        while (data[end] != '\0') end++;
        return Encoding.UTF8.GetString(data[start..end]);
    }

    // non extensions, just useful methods
    public static int StringCompare(ReadOnlySpan<byte> l, ReadOnlySpan<byte> r) {
        int i = 0;

        while (i < l.Length && i < r.Length && l[i] > 0 && r[i] > 0 && l[i] == r[i]) i++;

        if (i == l.Length && i == r.Length) return 0;
        if (i == l.Length || i == r.Length) return l.Length - r.Length;
        return l[i] - r[i];
    }

    public static int StringCompare(string l, string r) {
        return StringCompare(Encoding.UTF8.GetBytes(l), Encoding.UTF8.GetBytes(r));
    }
}