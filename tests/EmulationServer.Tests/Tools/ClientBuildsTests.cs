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
// File: tests/EmulationServer.Tests/Tools/ClientBuildsTests.cs
// Purpose: Contains client builds tests code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.Tools.Extraction.Client;

namespace EmulationServer.Tests.Tools;

// Type: ClientBuildsTests
// Purpose: Provides client builds tests behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ClientBuildsTests
{

    [Theory]
    [InlineData(5875)]
    [InlineData(6005)]
    [InlineData(6141)]
    [InlineData(8606)]
    [InlineData(12340)]
    // Method: IsSupported_ReturnsTrue_ForExpectedExtractorBuilds
    // Purpose: Validates or evaluates is supported returns true for expected extractor builds rules for the automated test and verification layer.
    // Parameters:
    // - build: Build value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ClientBuildsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void IsSupported_ReturnsTrue_ForExpectedExtractorBuilds(ushort build)
    {
        Assert.True(ClientBuilds.IsSupported(build));
    }

    [Fact]
    // Method: Require_Throws_ForUnsupportedBuild
    // Purpose: Executes the require throws for unsupported build operation for the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to ClientBuildsTests so callers do not duplicate validation, protocol, or persistence rules.
    public void Require_Throws_ForUnsupportedBuild()
    {
        Assert.Throws<NotSupportedException>(() => ClientBuilds.Require(15595));
    }
}
