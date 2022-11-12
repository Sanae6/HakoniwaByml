using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HakoniwaByml.Iter;

namespace HakoniwaByml;

internal static class Extensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Align(this int value, int mask) {
        return value + mask & ~mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Align(this long value, int mask) {
        return value + mask & ~mask;
    }

    public static bool IsRegularValue(this BymlDataType type) {
        return type is BymlDataType.Bool or BymlDataType.Int or BymlDataType.Uint or BymlDataType.Float
            or BymlDataType.String or BymlDataType.Null;
    }
    public static void Write<T>(this Stream stream, ref T value) where T : struct {
        Span<byte> data = stackalloc byte[Unsafe.SizeOf<T>()];
        MemoryMarshal.Write(data, ref value);
        stream.Write(data);
    }
    public static void Write<T>(this BinaryWriter writer, ref T value) where T : struct {
        writer.BaseStream.Write(ref value);
    }

    public static string ReadString(this ReadOnlySpan<byte> data, int start) {
        int end = start;
        while (data[end] != '\0') end++;
        return Encoding.UTF8.GetString(data[start..end]);
    }
}