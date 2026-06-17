//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// File: tests/EmulationServer.Tests/Tools/DbcFileTests.cs
// Purpose: Contains DBC file tests code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Text;
using EmulationServer.Tools.Extraction.Formats.Dbc;

namespace EmulationServer.Tests.Tools;

// Type: DbcFileTests
// Purpose: Provides DBC file tests behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class DbcFileTests
{

    [Fact]
    // Method: Load_ReadsHeaderRecordsAndStrings
    // Purpose: Retrieves load reads header records and strings data for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to DbcFileTests so callers do not duplicate validation, protocol, or persistence rules.
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
