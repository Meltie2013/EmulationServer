namespace EmulationServer.Game.Data.Dbc;

public sealed record DbcHeader(
    string Magic,
    int RecordCount,
    int FieldCount,
    int RecordSize,
    int StringBlockSize)
{
    public const string ExpectedMagic = "WDBC";

    public bool UsesFourByteFields => RecordSize == FieldCount * sizeof(uint);

    public bool UsesUniformCompactFields => TryGetUniformFieldSize(out _);

    public bool TryGetUniformFieldSize(out int fieldSize)
    {
        fieldSize = 0;

        if (FieldCount <= 0 || RecordSize <= 0 || RecordSize % FieldCount != 0)
        {
            return false;
        }

        fieldSize = RecordSize / FieldCount;
        return fieldSize is sizeof(byte) or sizeof(ushort) or sizeof(uint);
    }
}
