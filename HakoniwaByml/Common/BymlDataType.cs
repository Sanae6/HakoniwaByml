namespace HakoniwaByml.Iter;

public enum BymlDataType : byte {
    Invalid = 0x00,
    String = 0xA0,
    Binary = 0xA1,
    Array = 0xC0,
    Hash = 0xC1,
    StringTable = 0xC2,
    Bool = 0xD0,
    Int = 0xD1,
    Float = 0xD2,
    Uint = 0xD3,
    Long = 0xD4,
    Ulong = 0xD5,
    Double = 0xD6,
    Null = 0xFF
}