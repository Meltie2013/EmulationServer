
using System.Text;
using EmulationServer.Tools.Extraction.Formats.Dbc;

namespace EmulationServer.Tests.Tools;

public sealed class DbcFileTests
{
    [Fact]
    public void Load_ReadsHeaderRecordsAndStrings()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("WDBC"));
        writer.Write(1);
        writer.Write(2);
        writer.Write(8);
        writer.Write(6);
        writer.Write(123u);
        writer.Write(1u);
        writer.Write((byte)0);
        writer.Write(Encoding.UTF8.GetBytes("test"));
        writer.Write((byte)0);
        writer.Flush();

        stream.Position = 0;
        DbcFile dbc = DbcFile.Load(stream);
        DbcRecord record = dbc.GetRecord(0);

        Assert.Equal(1, dbc.RecordCount);
        Assert.Equal(2, dbc.FieldCount);
        Assert.Equal(123u, record.GetUInt32(0));
        Assert.Equal("test", record.GetString(1));
    }
}
