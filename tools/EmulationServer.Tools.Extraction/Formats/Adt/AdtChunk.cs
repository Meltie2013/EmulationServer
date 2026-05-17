
namespace EmulationServer.Tools.Extraction.Formats.Adt;

public readonly record struct AdtChunk(string FourCC, int Offset, int Size, int DataOffset);
