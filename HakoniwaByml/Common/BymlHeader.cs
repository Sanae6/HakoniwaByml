namespace HakoniwaByml.Common;

internal struct BymlHeader {
    public const ushort LittleEndianMarker = 0x4259;
    public ushort Tag { get; set; }
    public ushort Version { get; set; }
    public int HashKeyTableOffset { get; set; }
    public int StringTableOffset { get; set; }
    public int DataOffset { get; set; }
}