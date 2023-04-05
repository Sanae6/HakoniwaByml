using HakoniwaByml.Iter;

namespace HakoniwaByml.Common;

internal struct BymlHashPair {
    public int Key { get => Data & 0xFFFFFF; set => Data = (int) Type << 24 | value; }
    public BymlDataType Type { get => (BymlDataType) (Data >> 24); set => Data = Key | (int) value << 24; }
    public int Data;
    public int Value;
}